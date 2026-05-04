"""
AI-assisted data model generator for tenant schemas.

Hybrid approach:
- Deterministic: View discovery, SQL template generation (Form views, Worksheet/Scoresheet views)
- AI: Model naming/description, alias enhancement, fallback for unknown patterns

Workflow:
1. discover_views()  → List available Reporting views (Form, Worksheet, Scoresheet)
2. preview_model()   → Deterministic SQL generation (or AI fallback) + validation
3. create_models()   → Persist approved models as Metabase model cards

Supported patterns:
- Pattern 1 (Form Views): UNION ALL form versions with JSON unwrapping + type casts
- Pattern 2 (Worksheet/Scoresheet Views): Simple SELECT with JOIN to Applications
- Fallback: Full AI generation for unknown view types
"""
import json
import logging
import re
from collections import defaultdict
from typing import Optional

import aiohttp

from config import config
from metabase import metabase_client

CONTENT_TYPE = "application/json"

logger = logging.getLogger(__name__)

# --- Prompts ---

SYSTEM_PROMPT = (
    "You are a senior data analyst building a reusable reporting data model "
    "for a grants management database. You produce clean, well-aliased SQL."
)

MODEL_GENERATION_PROMPT = """Build a data model query for the worksheet view described below.

BASE VIEW: "{schema_name}"."{view_name}"
COLUMNS (name | type | label | sample value):
{columns_text}

AVAILABLE REFERENCE TABLES:
- "public"."Applications" (Id, ReferenceNo, ProjectName, ProjectSummary, RequestedAmount, TotalProjectBudget, EconomicRegion, RegionalDistrict, Community, Place, ProjectEndDate, SubmissionDate, ApplicationFormId, ApplicantId)
- "public"."Applicants" (Id, OrgName, OrgNumber)
- "public"."ApplicantAgents" (ApplicantId, ApplicationId, Name, Phone, Email)
- "public"."ApplicantAddresses" (ApplicantId, ApplicationId, Street, Unit, City, Province, Postal)

JOIN KEYS (columns that exist in the view for joining — do not SELECT these):
{join_keys_text}

RELATIONSHIPS (only use joins shown here):
{relationships_text}

RULES:
- Always include from reference tables: ReferenceNo, OrgName, ProjectName
- Include other useful reference columns (region, community, amounts) when relevant
- Exclude junk columns: any UUID/internal-key columns, CreatorId, timestamps (CreationTime, LastModificationTime), ExtraProperties, ConcurrencyStamp, CorrelationProvider
- Exclude the JOIN KEY columns from SELECT — they are only for JOIN conditions
- Include ALL worksheet columns in the SELECT (even those with no data yet) — do not exclude columns unless they are junk
- Use the provided Label as the column alias (AS "Label"). If no label exists, create a human-readable alias.
- Add computed columns where useful (e.g., date differences like CURRENT_DATE - CAST(col AS date) for "days since" calculations)
- Use LEFT JOINs to reference tables using ONLY the JOIN KEYS listed above
- Use PostgreSQL double-quoted identifiers for all table/column names
- Order columns logically: reference info first, then worksheet fields grouped by section
- If no JOIN KEYS are listed, query only the base view without any joins

Output ONLY valid JSON — no markdown fences, no explanation:
{{"name": "Short Model Name", "description": "One sentence explaining what this model provides for reporting", "sql": "SELECT ..."}}"""

SELF_HEAL_PROMPT = """The following SQL model query failed validation:

```sql
{bad_sql}
```

Error: {error}

Available view columns:
{columns_text}

JOIN KEYS (exist in view but must NOT be selected):
{join_keys_text}

Valid relationships:
{relationships_text}

Fix the SQL query. Output ONLY the corrected SQL — no explanation, no markdown fences."""

NAMING_PROMPT = """Given a data model built from source "{source}" with these columns:
{columns}

Provide ONLY valid JSON — no markdown fences, no explanation:
{{"name": "Short Model Name (2-4 words)", "description": "One sentence explaining what this model provides for reporting"}}"""

ENHANCE_PROMPT = """You are enhancing a data model SQL query. The query structure is LOCKED — do NOT change it.

RULES:
- Do NOT add or remove columns from the final SELECT
- Do NOT change column order
- Do NOT modify CTEs, FROM clauses, JOINs, UNION ALL, or WHERE clauses
- ONLY modify the final SELECT expressions:
  1. Replace raw field name aliases with human-readable labels:
     - Strip section prefixes (s1_, t3_, s02_, n01K, S01K, etc.)
     - Convert camelCase to Title Case with spaces
     - Example: "s1_simpletextfieldadvancedWide" → "Simple Text Field"
     - Example: "t7_totalProjectCost" → "Total Project Cost"
     - Example: "s2_selectBoolean" → "Select Boolean"
  2. Keep all existing type casts (::BOOLEAN, ::FLOAT) exactly as they are
  3. You may add computed columns at the END only (e.g., date differences)

CURRENT SQL:
{sql}

COLUMN SAMPLES (name | sample1 | sample2):
{samples}

Output ONLY the improved SQL — no markdown fences, no explanation."""

# Regex for parsing form view table names
FORM_VERSION_RE = re.compile(r'^Form-(.+?)(?:\s+Alternate)?-V(\d+)$')


class DataModelGenerator:
    """Hybrid AI-assisted data model generator using Reporting views."""

    JUNK_COLUMNS = {
        "Id", "CreatorId", "LastModificationTime",
        "LastModifierId", "ExtraProperties", "ConcurrencyStamp",
        "CreationTime", "CorrelationProvider",
        "WorksheetId", "WorksheetCorrelationId",
    }
    # CorrelationId intentionally NOT in JUNK_COLUMNS — used as FK for JOINs
    JUNK_PATTERNS = {"password", "ssn", "sin", "secret", "token"}

    # --- Source type detection ---

    def _detect_source_type(self, view_name: str, db_id: int, tenant_id: str) -> str:
        """Determine which pattern applies."""
        if view_name.startswith("Form-"):
            return "form_view"
        # Check if it's a base name that matches a form group
        metadata = metabase_client.get_database_metadata(db_id, tenant_id=tenant_id)
        form_groups = self._group_form_versions(metadata)
        if view_name in form_groups:
            return "form_view"
        # Worksheet/scoresheet views in Reporting schema
        name_lower = view_name.lower()
        if "worksheet" in name_lower or "scoresheet" in name_lower:
            return "worksheet_view"
        return "unknown"


    # --- Form version grouping ---

    def _group_form_versions(self, metadata: dict) -> dict[str, list[str]]:
        """
        Group Form-<base>-V<n> and Form-<base> Alternate-V<n> tables by base name.
        Returns {base_name: [table_name, ...]} sorted by version descending.
        """
        groups: dict[str, list[tuple[int, str]]] = defaultdict(list)

        for table in metadata.get("tables", []):
            if table.get("schema") != "Reporting":
                continue
            name = table.get("name", "")
            match = FORM_VERSION_RE.match(name)
            if match:
                base = match.group(1)
                version = int(match.group(2))
                groups[base].append((version, name))

        # Sort each group by version descending (primary version first)
        return {
            base: [name for _, name in sorted(versions, key=lambda x: -x[0])]
            for base, versions in groups.items()
        }

    # --- Type casting helpers ---

    def _type_cast_expr(self, col_ref: str, forms_type: str, alias: str) -> str:
        """Generate type-cast SQL expression based on forms_type."""
        ft = (forms_type or "").lower()
        if ft in ("boolean", "checkbox"):
            return f'{col_ref}::BOOLEAN AS "{alias}"'
        elif ft in ("number", "currency", "float", "integer"):
            return (
                f"CASE WHEN {col_ref} <> '' THEN {col_ref}::FLOAT\n"
                f'  ELSE 0\n  END AS "{alias}"'
            )
        elif ft == "date":
            # Keep as text — AI enhancement may add computed "days since" columns
            return f'{col_ref} AS "{alias}"'
        else:
            # Text passthrough
            return f'{col_ref} AS "{alias}"'


    def _needs_unwrap(self, schema: str, table: str, col_name: str,
                      db_id: int, tenant_id: str) -> bool:
        """Check if a column's sample value starts with '[\"' (JSON array wrapper)."""
        sample = self._get_sample_value(schema, table, col_name, db_id, tenant_id)
        return bool(sample and sample.startswith('["'))

    # --- Type inference ---

    def _infer_column_type(self, col_name: str, samples: list[str]) -> str:
        """Infer type from column name patterns and sample values."""
        name_lower = col_name.lower()
        # Name-based inference
        if any(k in name_lower for k in ("currency", "amount", "cost", "budget", "funding")):
            return "number"
        if any(k in name_lower for k in ("number", "count", "quantity", "total")):
            return "number"
        if any(k in name_lower for k in ("boolean", "confirm", "checkbox", "consent", "accept")):
            return "boolean"
        if any(k in name_lower for k in ("date", "time")):
            return "date"
        # Sample-based inference
        non_empty = [s for s in samples if s]
        for s in non_empty:
            if s.lower() in ("true", "false"):
                return "boolean"
        for s in non_empty:
            try:
                float(s)
                return "number"
            except (ValueError, TypeError):
                break  # if first sample isn't numeric, assume text
        return "text"

    def _get_column_samples(self, schema: str, table: str, columns: list[str],
                             db_id: int, tenant_id: str) -> dict[str, list[str]]:
        """Get 2-3 non-null sample values per column for type inference and AI context."""
        if not columns:
            return {}

        # Build a single query to get samples for all columns at once
        col_refs = ", ".join(f'"{c}"' for c in columns[:50])
        sql = (
            f'SELECT {col_refs} FROM "{schema}"."{table}" '
            f'LIMIT 3'
        )
        try:
            result = metabase_client.execute_sql(sql, db_id, tenant_id=tenant_id)
            rows = result.get("rows", [])
            samples: dict[str, list[str]] = {c: [] for c in columns[:50]}
            for row in rows:
                for i, col in enumerate(columns[:50]):
                    if i < len(row) and row[i] is not None and str(row[i]).strip():
                        val = str(row[i]).strip()
                        # Strip JSON wrapper if present for cleaner samples
                        if val.startswith('["') and val.endswith('"]'):
                            val = val[2:-2]
                        if val and len(samples[col]) < 3:
                            samples[col].append(val)
            return samples
        except Exception as e:
            logger.warning(f"Could not fetch column samples from {schema}.{table}: {e}")
            return {c: [] for c in columns[:50]}

    def _format_samples_for_prompt(self, samples: dict[str, list[str]]) -> str:
        """Format samples dict into a string for AI prompt."""
        lines = []
        for col, vals in samples.items():
            val_str = " | ".join(vals[:3]) if vals else "(empty)"
            lines.append(f"  {col} | {val_str}")
        return "\n".join(lines)

    # --- AI Enhancement ---

    async def _ai_enhance_sql(self, sql: str, view_name: str,
                               db_id: int, tenant_id: str,
                               primary_table: str = "") -> Optional[str]:
        """
        AI enhancement: improve aliases and suggest computed columns.
        Returns enhanced SQL or None if enhancement fails.
        """
        if not primary_table:
            primary_table = view_name

        # Get column names from the SQL (between combinedOri and FROM)
        columns = []
        for field_match in re.finditer(r'combinedOri\."([^"]+)"', sql):
            col = field_match.group(1)
            if col not in columns:
                columns.append(col)

        samples = self._get_column_samples("Reporting", primary_table, columns, db_id, tenant_id)
        samples_text = self._format_samples_for_prompt(samples)

        prompt = ENHANCE_PROMPT.format(sql=sql, samples=samples_text)

        async with aiohttp.ClientSession() as session:
            result = await self._post_completion(session, SYSTEM_PROMPT, prompt)

        if not result:
            return None

        # Clean markdown fences if present
        result = re.sub(r"^```(?:sql)?\s*", "", result.strip(), flags=re.IGNORECASE)
        result = re.sub(r"\s*```$", "", result.strip())
        return result.strip() or None

    async def _ai_enhance_worksheet_sql(self, sql: str, view_name: str,
                                          columns: list[str], db_id: int,
                                          tenant_id: str) -> Optional[str]:
        """
        AI enhancement for worksheet/scoresheet views.
        Returns enhanced SQL or None if enhancement fails.
        """
        samples = self._get_column_samples("Reporting", view_name, columns, db_id, tenant_id)
        samples_text = self._format_samples_for_prompt(samples)

        prompt = ENHANCE_PROMPT.format(sql=sql, samples=samples_text)

        async with aiohttp.ClientSession() as session:
            result = await self._post_completion(session, SYSTEM_PROMPT, prompt)

        if not result:
            return None

        result = re.sub(r"^```(?:sql)?\s*", "", result.strip(), flags=re.IGNORECASE)
        result = re.sub(r"\s*```$", "", result.strip())
        return result.strip() or None

    def _validate_enhancement(self, original_sql: str, enhanced_sql: str) -> bool:
        """Verify AI didn't modify structure — only aliases/casts in SELECT."""
        # For simple queries (no CTE), check FROM clause is preserved
        def extract_from_clause(sql: str) -> str:
            match = re.search(r'\bFROM\b(.+)$', sql, flags=re.IGNORECASE | re.DOTALL)
            return re.sub(r'\s+', ' ', match.group(1).strip()) if match else ""

        # For CTE queries, check CTE portion is preserved
        def extract_cte(sql: str) -> str:
            parts = re.split(r'\)\s*SELECT\b', sql, flags=re.IGNORECASE)
            if len(parts) >= 2:
                return parts[0]
            return ""

        has_cte = original_sql.strip().upper().startswith("WITH")

        if has_cte:
            original_cte = extract_cte(original_sql)
            enhanced_cte = extract_cte(enhanced_sql)
            if not original_cte or not enhanced_cte:
                return False
            norm_orig = re.sub(r'\s+', ' ', original_cte.strip())
            norm_enh = re.sub(r'\s+', ' ', enhanced_cte.strip())
            if norm_orig != norm_enh:
                logger.warning("AI enhancement modified CTE structure — rejecting")
                return False
        else:
            # Simple query — verify FROM clause unchanged
            orig_from = extract_from_clause(original_sql)
            enh_from = extract_from_clause(enhanced_sql)
            if orig_from != enh_from:
                logger.warning("AI enhancement modified FROM clause — rejecting")
                return False

        # Count columns in SELECT (handles commas inside CASE...END)
        def count_select_cols(sql: str) -> int:
            if has_cte:
                parts = re.split(r'\)\s*SELECT\b', sql, flags=re.IGNORECASE)
                if len(parts) >= 2:
                    final = parts[-1].split("FROM")[0] if "FROM" in parts[-1] else parts[-1]
                else:
                    return 0
            else:
                select_match = re.search(r'\bSELECT\b(.+?)\bFROM\b', sql, flags=re.IGNORECASE | re.DOTALL)
                if select_match:
                    final = select_match.group(1)
                else:
                    return 0
            # Remove CASE...END blocks to avoid counting their internal commas
            cleaned = re.sub(r'\bCASE\b.*?\bEND\b', 'X', final, flags=re.IGNORECASE | re.DOTALL)
            return cleaned.count(",") + 1

        orig_count = count_select_cols(original_sql)
        enh_count = count_select_cols(enhanced_sql)

        if enh_count < orig_count:
            logger.warning(
                "AI enhancement removed columns (%d → %d) — rejecting",
                orig_count, enh_count
            )
            return False

        return True

    # --- Deterministic SQL builders ---

    def _build_form_view_sql(self, base_name: str, db_id: int,
                             tenant_id: str) -> tuple[str, list[str]]:
        """
        Build deterministic SQL for Pattern 1 (Form Views).
        Returns (sql, column_list).
        """
        metadata = metabase_client.get_database_metadata(db_id, tenant_id=tenant_id)
        all_groups = self._group_form_versions(metadata)

        # Find the matching group for this base_name
        versions = all_groups.get(base_name, [])
        if not versions:
            # Try matching by full view name (user may pass the full table name)
            for base, tables in all_groups.items():
                if base_name in tables:
                    versions = tables
                    base_name = base
                    break
            if not versions:
                return "", []

        # Get columns from the primary (highest version) table
        primary_table = versions[0]
        primary_fields = []
        for table in metadata.get("tables", []):
            if table.get("name") == primary_table and table.get("schema") == "Reporting":
                primary_fields = table.get("fields", [])
                break

        # Get labels
        labels = self._get_view_labels(primary_table, db_id, tenant_id)

        # Identify columns to include (exclude junk)
        columns = []
        for field in primary_fields:
            col_name = field.get("name", "")
            if col_name in self.JUNK_COLUMNS or col_name == "ApplicationId":
                continue
            if any(p in col_name.lower() for p in self.JUNK_PATTERNS):
                continue
            columns.append(col_name)

        if not columns:
            return "", []

        # Check if columns need JSON unwrapping (sample from primary table)
        needs_unwrap = self._needs_unwrap("Reporting", primary_table, columns[0], db_id, tenant_id)

        # Build UNION ALL CTE parts
        union_parts = []
        for i, table_name in enumerate(versions):
            alias = f"ori{i + 1}"
            if needs_unwrap:
                col_exprs = [f'{alias}."Id" AS "Id"', f'{alias}."ApplicationId" AS "ApplicationId"']
                for col in columns:
                    col_exprs.append(
                        f'CASE WHEN LENGTH({alias}."{col}") > 2 '
                        f'THEN SUBSTRING({alias}."{col}", 3, LENGTH({alias}."{col}") - 4) END AS "{col}"'
                    )
            else:
                col_exprs = [f'{alias}."Id" AS "Id"', f'{alias}."ApplicationId" AS "ApplicationId"']
                for col in columns:
                    col_exprs.append(f'{alias}."{col}" AS "{col}"')

            select_clause = ",\n".join(col_exprs)
            union_parts.append(
                f"SELECT\n{select_clause}\nFROM\n"
                f'  "Reporting"."{table_name}" AS {alias}'
            )

        combined_cte = "\n  UNION ALL\n  ".join(union_parts)

        # Get samples for type inference
        samples = self._get_column_samples("Reporting", primary_table, columns, db_id, tenant_id)

        # Build final SELECT with type casts
        final_exprs = ['a."ReferenceNo"']
        for col in columns:
            label_info = labels.get(col, {})
            label = label_info.get("label", col)
            forms_type = label_info.get("forms_type", "")
            # If no forms_type from labels, infer from name + samples
            if not forms_type or forms_type == "text":
                forms_type = self._infer_column_type(col, samples.get(col, []))
            col_ref = f'combinedOri."{col}"'
            final_exprs.append(self._type_cast_expr(col_ref, forms_type, label))

        final_select = ",\n  ".join(final_exprs)

        sql = (
            f'WITH a AS (\n'
            f'    SELECT\n      "public"."Applications"."Id",\n'
            f'      "public"."Applications"."ReferenceNo"\n'
            f'    FROM\n      "public"."Applications"\n'
            f'  ),\n'
            f'  combinedOri AS ({combined_cte})\n'
            f'  SELECT\n  {final_select}\n'
            f'  FROM\n  combinedOri\n'
            f'  LEFT JOIN a ON a."Id" = combinedOri."ApplicationId"'
        )

        column_labels = [labels.get(c, {}).get("label", c) for c in columns]
        return sql, column_labels

    def _build_worksheet_view_sql(self, view_name: str, db_id: int,
                                   tenant_id: str) -> tuple[str, list[str]]:
        """
        Build deterministic SQL for worksheet/scoresheet views.
        Simpler than form views: no UNION ALL, no JSON unwrap.
        Just SELECT columns with JOIN to Applications for ReferenceNo.
        Returns (sql, column_list).
        """
        metadata = metabase_client.get_database_metadata(db_id, tenant_id=tenant_id)

        # Find the view
        target_fields = []
        for table in metadata.get("tables", []):
            if table.get("name") == view_name and table.get("schema") == "Reporting":
                target_fields = table.get("fields", [])
                break

        if not target_fields:
            return "", []

        # Get labels
        labels = self._get_view_labels(view_name, db_id, tenant_id)
        if not labels:
            custom_labels = self._get_custom_field_labels(db_id, tenant_id)
        else:
            custom_labels = {}

        # Detect FK column for JOIN
        fk_column = None
        field_names = {f.get("name", "") for f in target_fields}
        for candidate in ("ApplicationId", "CorrelationId"):
            if candidate in field_names:
                fk_column = candidate
                break
        # Last resort: query for it
        if not fk_column:
            fk_column = self._find_application_id_column("Reporting", view_name, db_id, tenant_id)

        # Determine JOIN target
        # Both ApplicationId and CorrelationId map to Applications.Id
        if fk_column:
            join_condition = f'"t"."{fk_column}" = a."Id"'
        else:
            join_condition = None

        # Identify columns to include (exclude junk + FK columns)
        columns = []
        skip_cols = self.JUNK_COLUMNS | ({fk_column} if fk_column else set())
        for field in target_fields:
            col_name = field.get("name", "")
            if col_name in skip_cols:
                continue
            if any(p in col_name.lower() for p in self.JUNK_PATTERNS):
                continue
            columns.append(col_name)

        if not columns:
            return "", []

        # Get samples for type inference
        samples = self._get_column_samples("Reporting", view_name, columns, db_id, tenant_id)

        # Build SELECT expressions
        select_exprs = []
        if join_condition:
            select_exprs.append('a."ReferenceNo"')

        for col in columns:
            label_info = labels.get(col, {})
            label = label_info.get("label", "") or custom_labels.get(col, "") or col
            forms_type = label_info.get("forms_type", "")
            if not forms_type or forms_type == "text":
                forms_type = self._infer_column_type(col, samples.get(col, []))
            col_ref = f'"t"."{col}"'
            select_exprs.append(self._type_cast_expr(col_ref, forms_type, label))

        select_clause = ",\n  ".join(select_exprs)

        if join_condition:
            sql = (
                f'SELECT\n  {select_clause}\n'
                f'FROM\n  "Reporting"."{view_name}" AS "t"\n'
                f'LEFT JOIN "public"."Applications" AS a ON {join_condition}'
            )
        else:
            sql = (
                f'SELECT\n  {select_clause}\n'
                f'FROM\n  "Reporting"."{view_name}" AS "t"'
            )

        column_labels = []
        for col in columns:
            label_info = labels.get(col, {})
            label = label_info.get("label", "") or custom_labels.get(col, "") or col
            column_labels.append(label)

        return sql, column_labels

    # --- AI naming helper ---

    async def _generate_name_description(self, source: str, columns: list[str],
                                          db_id: int, tenant_id: str) -> tuple[str, str]:
        """Use AI only for naming and description."""
        columns_preview = ", ".join(columns[:25])
        prompt = NAMING_PROMPT.format(source=source, columns=columns_preview)

        async with aiohttp.ClientSession() as session:
            raw = await self._post_completion(session, SYSTEM_PROMPT, prompt)

        if raw:
            try:
                defn = self._parse_single_definition(raw)
                return defn["name"], defn["description"]
            except (json.JSONDecodeError, KeyError, ValueError):
                pass

        # Fallback: derive from source name
        clean_name = re.sub(r'-V\d+$', '', source).replace("-", " ").strip()
        clean_name = re.sub(r'^Form-\s*', '', clean_name)
        return clean_name, f"Data model from {source}"

    def discover_views(self, db_id: int, tenant_id: str) -> list[dict]:
        """
        Step 0: Discover available Reporting schema views (Worksheet, Scoresheet, Form).

        Returns list of {view_name, display_name, column_count, has_labels, source_type, ...}.
        """
        metadata = metabase_client.get_database_metadata(db_id, tenant_id=tenant_id)
        views = []

        for table in metadata.get("tables", []):
            if table.get("schema") != "Reporting":
                continue
            name = table.get("name", "")

            # Detect source type by prefix/pattern
            if name.startswith("Form-"):
                source_type = "form_view"
                match = FORM_VERSION_RE.match(name)
                if match:
                    display_name = match.group(1)
                    version = f"V{match.group(2)}"
                else:
                    display_name = name.replace("Form-", "")
                    version = ""
            elif "worksheet" in name.lower():
                source_type = "worksheet_view"
                display_name = name
                version = ""
            elif "scoresheet" in name.lower():
                source_type = "scoresheet_view"
                display_name = name
                version = ""
            else:
                continue

            fields = table.get("fields", [])
            column_count = sum(
                1 for f in fields if f.get("name") not in self.JUNK_COLUMNS
            )

            if not self._has_data("Reporting", name, db_id, tenant_id):
                continue

            has_labels = self._has_labels(name, db_id, tenant_id)

            entry = {
                "view_name": name,
                "display_name": display_name,
                "column_count": column_count,
                "has_labels": has_labels,
                "source_type": source_type,
            }
            if source_type == "form_view":
                entry["form_group"] = display_name
                entry["version"] = version

            views.append(entry)

        views.sort(key=lambda v: v["view_name"])
        logger.info(f"Discovered {len(views)} views for tenant {tenant_id}")
        return views

    async def preview_model(self, view_name: str, db_id: int,
                            tenant_id: str) -> dict:
        """
        Step 1: Generate a model proposal for a selected view.

        Routes to deterministic builders for known patterns, falls back to AI.
        """
        source_type = self._detect_source_type(view_name, db_id, tenant_id)

        if source_type == "form_view":
            # Extract base name from full table name if needed
            match = FORM_VERSION_RE.match(view_name)
            base_name = match.group(1) if match else view_name
            sql, columns = self._build_form_view_sql(base_name, db_id, tenant_id)

            # AI enhancement: improve aliases and suggest computed columns
            if sql:
                enhanced_sql = await self._ai_enhance_sql(sql, view_name, db_id, tenant_id, primary_table=view_name)
                if enhanced_sql and self._validate_enhancement(sql, enhanced_sql):
                    sql = enhanced_sql
        elif source_type == "worksheet_view":
            sql, columns = self._build_worksheet_view_sql(view_name, db_id, tenant_id)

            # AI enhancement for worksheet views too
            if sql:
                enhanced_sql = await self._ai_enhance_worksheet_sql(sql, view_name, columns, db_id, tenant_id)
                if enhanced_sql and self._validate_enhancement(sql, enhanced_sql):
                    sql = enhanced_sql
        else:
            return await self._preview_model_ai(view_name, db_id, tenant_id)

        if not sql:
            return {
                "name": view_name,
                "description": "Could not build model SQL",
                "sql": "",
                "valid": False,
                "error": "No columns or source data found",
                "source_view": view_name,
                "columns": [],
                "excluded_columns": [],
            }

        # Validate deterministic SQL
        is_valid, error = metabase_client.validate_sql(sql, db_id, tenant_id=tenant_id)

        if not is_valid:
            # Self-heal as safety net
            healed_sql = await self._self_heal(
                sql, error or "", "\n".join(columns), "(deterministic)", "(deterministic)"
            )
            if healed_sql:
                is_valid, heal_error = metabase_client.validate_sql(
                    healed_sql, db_id, tenant_id=tenant_id
                )
                if is_valid:
                    sql = healed_sql
                    error = heal_error

        # AI for naming/description only
        name, description = await self._generate_name_description(
            view_name, columns, db_id, tenant_id
        )

        proposal = {
            "name": name,
            "description": description,
            "sql": sql,
            "valid": is_valid,
            "error": error,
            "source_view": view_name,
            "columns": columns,
            "excluded_columns": [],
        }

        logger.info(
            "Model preview complete (deterministic/%s) - tenant=%s view=%s valid=%s",
            source_type, tenant_id, view_name, is_valid
        )
        return proposal

    async def _preview_model_ai(self, view_name: str, db_id: int,
                                 tenant_id: str) -> dict:
        """
        Fallback: Full AI-based model generation for unknown source types.
        (Original preview_model logic.)
        """
        # Build the AI prompt context
        columns_text, schema_name, join_keys_text, relationships_text = self._build_prompt_context(
            view_name, db_id, tenant_id
        )

        if not columns_text:
            return {
                "name": view_name,
                "description": "Could not extract column information",
                "sql": "",
                "valid": False,
                "error": "No columns found for this view",
                "source_view": view_name,
                "columns": [],
                "excluded_columns": [],
            }

        # AI generates the model SQL
        prompt = MODEL_GENERATION_PROMPT.format(
            schema_name=schema_name,
            view_name=view_name,
            columns_text=columns_text,
            join_keys_text=join_keys_text,
            relationships_text=relationships_text,
        )

        definition = await self._generate_single(prompt)

        if not definition:
            return {
                "name": view_name,
                "description": "AI generation failed",
                "sql": "",
                "valid": False,
                "error": "Failed to generate model definition",
                "source_view": view_name,
                "columns": [],
                "excluded_columns": [],
            }

        # Validate SQL
        is_valid, error = metabase_client.validate_sql(
            definition["sql"], db_id, tenant_id=tenant_id
        )

        # Self-heal on failure
        if not is_valid:
            healed_sql = await self._self_heal(
                definition["sql"], error or "", columns_text, join_keys_text, relationships_text
            )
            if healed_sql:
                is_valid, error = metabase_client.validate_sql(
                    healed_sql, db_id, tenant_id=tenant_id
                )
                if is_valid:
                    definition["sql"] = healed_sql

        # Extract column lists from the generated SQL
        columns, excluded = self._extract_column_info(
            view_name, db_id, tenant_id, definition["sql"]
        )

        proposal = {
            "name": definition["name"],
            "description": definition["description"],
            "sql": definition["sql"],
            "valid": is_valid,
            "error": error,
            "source_view": view_name,
            "columns": columns,
            "excluded_columns": excluded,
        }

        logger.info(
            "Model preview complete - tenant=%s view=%s valid=%s",
            tenant_id, view_name, is_valid
        )
        return proposal

    def create_models(self, definitions: list[dict], db_id: int,
                      collection_id: int, tenant_id: str) -> dict:
        """Step 2: Create user-approved models in Metabase with partial failure handling."""
        existing_names = metabase_client.get_all_card_names(tenant_id)

        created = []
        errors = []
        for defn in definitions:
            name = defn["name"]
            if name in existing_names:
                errors.append({"name": name, "error": f"Model '{name}' already exists"})
                continue
            try:
                card_id = metabase_client.create_model(
                    defn["sql"], db_id, collection_id,
                    name, defn.get("description", ""), tenant_id=tenant_id
                )
                created.append({
                    "name": name,
                    "description": defn.get("description", ""),
                    "card_id": card_id,
                })
            except Exception as e:
                logger.error(f"Failed to create model '{name}'", exc_info=True)
                errors.append({"name": name, "error": str(e)})

        logger.info(
            "Model creation complete - tenant=%s models_created=%d models_errored=%d",
            tenant_id, len(created), len(errors)
        )
        return {"created": created, "errors": errors}

    # --- Private helpers ---

    def _build_prompt_context(self, view_name: str, db_id: int,
                              tenant_id: str) -> tuple[str, str, str, str]:
        """
        Assemble column info for the AI prompt using live Metabase metadata.

        Resolves FK relationships from fk_target_field_id — no hardcoded column names.

        Returns (columns_text, schema_name, join_keys_text, relationships_text).
        """
        metadata = metabase_client.get_database_metadata(db_id, tenant_id=tenant_id)

        # Build a field-ID → (schema, table, column) lookup across all tables
        field_lookup: dict[int, tuple[str, str, str]] = {}
        for table in metadata.get("tables", []):
            t_schema = table.get("schema", "public")
            t_name = table.get("name", "")
            for field in table.get("fields", []):
                fid = field.get("id")
                if fid:
                    field_lookup[fid] = (t_schema, t_name, field.get("name", ""))

        # Find the target view
        target_table = None
        for table in metadata.get("tables", []):
            if table.get("name") == view_name and table.get("schema") == "Reporting":
                target_table = table
                break

        if not target_table:
            logger.error(f"View '{view_name}' not found in metadata")
            return "", "Reporting", "(none)", "(none — query the view directly without joins)"

        schema_name = target_table.get("schema", "Reporting")
        fields = target_table.get("fields", [])

        # Fetch labels from ReportColumnsMaps, fall back to CustomFields
        view_labels = self._get_view_labels(view_name, db_id, tenant_id)
        custom_labels = {} if view_labels else self._get_custom_field_labels(db_id, tenant_id)

        # Separate fields into FK join keys and displayable columns
        fk_map: dict[str, tuple[str, str, str]] = {}  # col_name → (tgt_schema, tgt_table, tgt_col)
        field_names = {f.get("name", "") for f in fields}
        for field in fields:
            col_name = field.get("name", "")
            fk_target_id = field.get("fk_target_field_id")
            if fk_target_id and fk_target_id in field_lookup:
                fk_map[col_name] = field_lookup[fk_target_id]

        # Fallback: detect common FK columns by naming convention if Metabase has no FK metadata
        if not fk_map:
            KNOWN_FKS = {
                "ApplicationId": ("public", "Applications", "Id"),
                "CorrelationId": ("public", "Applications", "Id"),
                "ApplicantId": ("public", "Applicants", "Id"),
            }
            for col_name, target in KNOWN_FKS.items():
                if col_name in field_names:
                    fk_map[col_name] = target

        # Last resort: query the actual view to check for ApplicationId column
        if not fk_map:
            app_id_col = self._find_application_id_column(schema_name, view_name, db_id, tenant_id)
            if app_id_col:
                fk_map[app_id_col] = ("public", "Applications", "Id")
                field_names.add(app_id_col)

        logger.info(f"FK map for {view_name}: {fk_map}")
        join_key_names = set(fk_map.keys()) | (self.JUNK_COLUMNS & {f.get("name", "") for f in fields})

        lines = []
        for field in fields:
            col_name = field.get("name", "")
            if col_name in join_key_names:
                continue
            if any(p in col_name.lower() for p in self.JUNK_PATTERNS):
                continue

            db_type = field.get("database_type") or field.get("base_type", "unknown")
            label = (
                view_labels.get(col_name, {}).get("label", "")
                or custom_labels.get(col_name, "")
            )
            sample = self._get_sample_value(schema_name, view_name, col_name, db_id, tenant_id)
            sample_str = f'"{sample[:60]}"' if sample else "(no data yet)"
            lines.append(f"  {col_name} | {db_type} | {label or '(no label)'} | {sample_str}")

        # Build join keys and relationships from actual FK metadata
        if fk_map:
            fk_lines = [
                f'  "{col}" (FK → "{tgt_schema}"."{tgt_table}"."{tgt_col}" — JOIN only, do not SELECT)'
                for col, (tgt_schema, tgt_table, tgt_col) in sorted(fk_map.items())
            ]
            join_keys_text = "\n".join(fk_lines)

            rel_lines = [
                f'- "{view_name}"."{col}" = "{tgt_schema}"."{tgt_table}"."{tgt_col}"'
                for col, (tgt_schema, tgt_table, tgt_col) in sorted(fk_map.items())
            ]
            # Add transitive joins through known reference tables
            fk_targets = {(s, t) for s, t, _ in fk_map.values()}
            if ("public", "Applications") in fk_targets:
                rel_lines.append('- "public"."Applications"."ApplicantId" → "public"."Applicants"."Id"')
                rel_lines.append('- "public"."ApplicantAgents"."ApplicantId" = "public"."Applicants"."Id" AND "public"."ApplicantAgents"."ApplicationId" = "public"."Applications"."Id"')
                rel_lines.append('- "public"."ApplicantAddresses"."ApplicantId" = "public"."Applicants"."Id" AND "public"."ApplicantAddresses"."ApplicationId" = "public"."Applications"."Id"')
            relationships_text = "\n".join(rel_lines)
        else:
            join_keys_text = "(none — this view has no FK columns; query directly without joins)"
            relationships_text = "(none — query the view directly without joins)"

        return "\n".join(lines), schema_name, join_keys_text, relationships_text

    def _has_data(self, schema: str, table: str, db_id: int, tenant_id: str) -> bool:
        """Check if a view/table has at least one row."""
        sql = f'SELECT 1 FROM "{schema}"."{table}" LIMIT 1'
        try:
            result = metabase_client.execute_sql(sql, db_id, tenant_id=tenant_id)
            return bool(result.get("rows"))
        except Exception:
            return False


    def _has_labels(self, view_name: str, db_id: int, tenant_id: str) -> bool:
        """Check if ReportColumnsMaps has entries for this view."""
        sql = f"""
        SELECT 1 FROM "Reporting"."ReportColumnsMaps"
        WHERE "ViewName" = '{view_name}' LIMIT 1
        """
        try:
            result = metabase_client.execute_sql(sql, db_id, tenant_id=tenant_id)
            return bool(result.get("rows"))
        except Exception:
            return False

    def _get_view_labels(self, view_name: str, db_id: int,
                         tenant_id: str) -> dict:
        """Fetch {col_name: {label, forms_type}} from ReportColumnsMaps."""
        sql = f"""
        SELECT
            row_data->>'ColumnName' AS column_name,
            row_data->>'Label'      AS label,
            row_data->>'Type'       AS forms_type
        FROM "Reporting"."ReportColumnsMaps" rcm,
             jsonb_array_elements(rcm."Mapping"->'Rows') AS row_data
        WHERE rcm."ViewName" = '{view_name}'
        """
        try:
            result = metabase_client.execute_sql(sql, db_id, tenant_id=tenant_id)
            return {
                row[0]: {"label": row[1], "forms_type": row[2]}
                for row in result.get("rows", [])
                if row[0]
            }
        except Exception as e:
            logger.warning(f"Could not fetch labels for view {view_name}: {e}")
            return {}

    def _get_custom_field_labels(self, db_id: int, tenant_id: str) -> dict:
        """Fallback: fetch {key: label} from Flex.CustomFields."""
        sql = 'SELECT "Key", "Label" FROM "Flex"."CustomFields" WHERE "Key" IS NOT NULL'
        try:
            result = metabase_client.execute_sql(sql, db_id, tenant_id=tenant_id)
            return {row[0]: row[1] for row in result.get("rows", []) if row[0]}
        except Exception as e:
            logger.warning(f"Could not fetch custom field labels: {e}")
            return {}

    def _find_application_id_column(self, schema: str, view_name: str,
                                     db_id: int, tenant_id: str) -> Optional[str]:
        """Check if the view has an ApplicationId column by querying it directly."""
        sql = f'SELECT "ApplicationId" FROM "{schema}"."{view_name}" LIMIT 1'
        try:
            result = metabase_client.execute_sql(sql, db_id, tenant_id=tenant_id)
            if "error" not in result:
                logger.info(f"Found ApplicationId column in {schema}.{view_name}")
                return "ApplicationId"
        except Exception as e:
            logger.debug(f"ApplicationId not found in {schema}.{view_name}: {e}")
        return None

    def _get_sample_value(self, schema: str, table: str, column: str,
                          db_id: int, tenant_id: str) -> Optional[str]:
        """Get a single sample value for a column."""
        sql = (
            f'SELECT "{column}" FROM "{schema}"."{table}" '
            f"WHERE \"{column}\" IS NOT NULL AND \"{column}\" <> '' LIMIT 1"
        )
        try:
            result = metabase_client.execute_sql(sql, db_id, tenant_id=tenant_id)
            rows = result.get("rows", [])
            if rows and rows[0][0]:
                return str(rows[0][0])
        except Exception:
            pass
        return None

    def _extract_column_info(self, view_name: str, db_id: int,
                             tenant_id: str, sql: str) -> tuple[list[str], list[str]]:
        """
        Compare view columns against generated SQL to determine included/excluded.

        Returns (included_columns, excluded_columns).
        """
        metadata = metabase_client.get_database_metadata(db_id, tenant_id=tenant_id)

        all_columns = set()
        for table in metadata.get("tables", []):
            if table.get("name") == view_name and table.get("schema") == "Reporting":
                all_columns = {
                    f.get("name") for f in table.get("fields", [])
                    if f.get("name") not in self.JUNK_COLUMNS
                }
                break

        # Find which view columns appear in the SQL
        included = []
        excluded = []
        sql_upper = sql.upper()
        for col in sorted(all_columns):
            if f'"{col}"' in sql or col.upper() in sql_upper:
                included.append(col)
            else:
                excluded.append(col)

        return included, excluded

    async def _generate_single(self, prompt: str) -> Optional[dict]:
        """Call AI to generate a single model definition."""
        async with aiohttp.ClientSession() as session:
            for attempt in range(3):
                raw = await self._post_completion(session, SYSTEM_PROMPT, prompt)
                if not raw:
                    logger.warning(f"Generation attempt {attempt + 1} returned None")
                    continue
                try:
                    return self._parse_single_definition(raw)
                except (json.JSONDecodeError, KeyError, ValueError) as e:
                    logger.warning(f"Parse attempt {attempt + 1} failed: {e}")
        return None

    async def _self_heal(self, bad_sql: str, error: str, columns_text: str,
                         join_keys_text: str, relationships_text: str) -> Optional[str]:
        """Feed failed SQL + error back to AI for one correction attempt."""
        prompt = SELF_HEAL_PROMPT.format(
            bad_sql=bad_sql, error=error, columns_text=columns_text,
            join_keys_text=join_keys_text, relationships_text=relationships_text,
        )
        async with aiohttp.ClientSession() as session:
            result = await self._post_completion(session, SYSTEM_PROMPT, prompt)
        if not result:
            return None
        result = re.sub(r"^```sql\s*", "", result.strip(), flags=re.IGNORECASE)
        result = re.sub(r"\s*```$", "", result.strip())
        return result.strip() or None

    async def _post_completion(self, session: aiohttp.ClientSession,
                               system_message: str, prompt: str) -> Optional[str]:
        """Call the configured LLM."""
        ai_cfg = config.ai

        if ai_cfg.use_azure:
            headers = {
                "api-key": ai_cfg.azure_api_key,
                "Content-Type": CONTENT_TYPE,
            }
            endpoint = (
                f"{ai_cfg.azure_endpoint}/openai/deployments/"
                f"{ai_cfg.azure_deployment}/chat/completions"
                f"?api-version={ai_cfg.azure_api_version}"
            )
            json_data = {
                "messages": [
                    {"role": "system", "content": system_message},
                    {"role": "user", "content": prompt},
                ],
                "max_completion_tokens": 4000,
            }
        else:
            headers = {
                "Authorization": f"Bearer {ai_cfg.completion_key}",
                "Content-Type": CONTENT_TYPE,
            }
            endpoint = ai_cfg.completion_endpoint
            json_data = {
                "model": ai_cfg.model,
                "messages": [
                    {"role": "system", "content": system_message},
                    {"role": "user", "content": prompt},
                ],
                "max_completion_tokens": 4000,
            }

        try:
            timeout = aiohttp.ClientTimeout(total=120)
            async with session.post(endpoint, headers=headers, json=json_data, timeout=timeout) as response:
                if response.status != 200:
                    logger.error(f"LLM error: {response.status} {await response.text()}")
                    return None
                data = await response.json()
                return data["choices"][0]["message"]["content"]
        except Exception as e:
            logger.error(f"LLM request failed: {e}", exc_info=True)
            return None

    def _parse_single_definition(self, raw: str) -> dict:
        """Parse AI response into a single {name, description, sql} dict."""
        text = re.sub(r"^```(?:json)?\s*", "", raw.strip(), flags=re.IGNORECASE)
        text = re.sub(r"\s*```$", "", text.strip())

        # Find JSON object
        start = text.find("{")
        end = text.rfind("}")
        if start == -1 or end == -1 or end <= start:
            raise ValueError("No JSON object found in response")

        definition = json.loads(text[start:end + 1])

        for key in ("name", "description", "sql"):
            if not isinstance(definition.get(key), str) or not definition[key].strip():
                raise KeyError(f"Missing required field: {key}")

        return {
            "name": definition["name"].strip(),
            "description": definition["description"].strip(),
            "sql": definition["sql"].strip(),
        }


# Global singleton
data_model_generator = DataModelGenerator()
