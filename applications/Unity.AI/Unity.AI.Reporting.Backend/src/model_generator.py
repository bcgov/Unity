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
import threading
import time
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

# Combined alias-enhancement + naming in a single LLM call. Used when the
# deterministic SQL still has raw (unlabeled) aliases, so we pay for one call
# instead of separate enhance + naming calls. Same structure-lock rules as
# ENHANCE_PROMPT; output adds the {name, description} the naming call produced.
ENHANCE_AND_NAME_PROMPT = """You are finalizing a reusable reporting data model. The query structure is LOCKED — do NOT change it.

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
  2. Keep all existing type casts (::BOOLEAN, ::FLOAT) exactly as they are
  3. You may add computed columns at the END only (e.g., date differences)

CURRENT SQL:
{sql}

COLUMN SAMPLES (name | sample1 | sample2):
{samples}

Also provide a short model name (2-4 words) and a one-sentence description of what it provides for reporting.

Output ONLY valid JSON — no markdown fences, no explanation:
{{"name": "Short Model Name", "description": "One sentence explaining what this model provides for reporting", "sql": "SELECT ..."}}"""

MODIFY_PROMPT = """You are modifying an existing data model SQL query.

CURRENT SQL:
{current_sql}

CURRENT COLUMNS (preserve unless user explicitly asks to remove them):
{current_columns}

USER'S CHANGE REQUEST:
{user_prompt}

CORE FIELDS TO INCLUDE (from "public"."Applications" — ensure these are present
in the final SELECT, adding a LEFT JOIN if not already joined):
{core_fields_text}

ADDITIONAL VIEW SQL TO INTEGRATE (wrap each as a named CTE and LEFT JOIN to the
existing SQL via "ReferenceNo"; if a view has no "ReferenceNo" column, omit it):
{additional_views_text}

RULES:
- Keep all existing columns from CURRENT SQL unless the user explicitly asked to drop them
- Use PostgreSQL double-quoted identifiers
- When integrating additional views, wrap them as named CTEs (view_a, view_b, ...) and
  LEFT JOIN on "ReferenceNo" — never CROSS JOIN
- If the user prompt asks for a computed column, add it at the end of the SELECT list
- Do not invent table or column names not present in CURRENT SQL or ADDITIONAL VIEW SQL
- For CORE FIELDS TO INCLUDE: ensure each listed column is in the SELECT, using
  exactly the alias shown after the "→" arrow (do not invent your own alias).
  If "public"."Applications" is not yet joined, add a LEFT JOIN via
  "ApplicationId" or "CorrelationId" — never CROSS JOIN
- Remove any column whose alias ends with "_id" (case-insensitive snake_case suffix);
  these are internal FK reference columns and should not appear in the final SELECT

Output ONLY the rewritten SQL — no markdown fences, no explanation."""

# Regex for parsing form view table names
FORM_VERSION_RE = re.compile(r'^Form-(.+?)(?:\s+Alternate)?-V(\d+)$')


class DataModelGenerator:
    """Hybrid AI-assisted data model generator using Reporting views."""

    # Short TTL cache for Metabase database metadata. The same metadata is fetched
    # multiple times within a single preview (and once per view for combined models);
    # a brief TTL de-dupes those round-trips while staying fresh for schema edits.
    _META_CACHE_TTL = 60  # seconds

    def __init__(self) -> None:
        # key: (db_id, tenant_id) → (fetched_at_monotonic, metadata_dict)
        self._meta_cache: dict[tuple[int, str], tuple[float, dict]] = {}
        self._meta_lock = threading.Lock()
        # ReportColumnsMaps info (label set + correlation-provider map), same TTL —
        # de-dupes the per-view source-type lookups within a single preview.
        self._rcm_cache: dict[tuple[int, str], tuple[float, tuple[set, dict]]] = {}
        self._rcm_lock = threading.Lock()

    def _get_metadata(self, db_id: int, tenant_id: str) -> dict:
        """Return Metabase DB metadata, cached per (db_id, tenant_id) for a short TTL.

        Callers treat the returned dict as read-only — it is shared across the cache
        window, so it must not be mutated.
        """
        key = (int(db_id), str(tenant_id).strip())
        now = time.monotonic()
        with self._meta_lock:
            cached = self._meta_cache.get(key)
            if cached and (now - cached[0]) < self._META_CACHE_TTL:
                return cached[1]
            metadata = metabase_client.get_database_metadata(db_id, tenant_id=tenant_id)
            self._meta_cache[key] = (now, metadata)
            return metadata

    JUNK_COLUMNS = {
        "Id", "CreatorId", "LastModificationTime",
        "LastModifierId", "ExtraProperties", "ConcurrencyStamp",
        "CreationTime", "CorrelationProvider",
        "WorksheetId", "WorksheetCorrelationId",
    }
    # CorrelationId intentionally NOT in JUNK_COLUMNS — used as FK for JOINs
    JUNK_PATTERNS = {"password", "ssn", "sin", "secret", "token"}

    # Curated columns from public.Applications that users can opt-in to via the
    # "Core fields" picker. ReferenceNo is required as JOIN key for combined
    # multi-view models, so it's the default selection.
    CORE_FIELDS = [
        {"name": "ReferenceNo",        "label": "Reference No",         "type": "text",   "default_selected": True},
        {"name": "ProjectName",        "label": "Project Name",         "type": "text",   "default_selected": False},
        {"name": "ProjectSummary",     "label": "Project Summary",      "type": "text",   "default_selected": False},
        {"name": "RequestedAmount",    "label": "Requested Amount",     "type": "number", "default_selected": False},
        {"name": "TotalProjectBudget", "label": "Total Project Budget", "type": "number", "default_selected": False},
        {"name": "SubmissionDate",     "label": "Submission Date",      "type": "date",   "default_selected": False},
        {"name": "ProjectEndDate",     "label": "Project End Date",     "type": "date",   "default_selected": False},
    ]

    def get_core_fields(self) -> list[dict]:
        """Return the curated list of public.Applications columns users can include."""
        return list(self.CORE_FIELDS)

    def _default_core_field_names(self) -> list[str]:
        return [cf["name"] for cf in self.CORE_FIELDS if cf["default_selected"]]

    def _resolve_core_fields(self, core_fields: Optional[list[str]]) -> list[dict]:
        """Map requested core-field names to their CORE_FIELDS dicts, preserving order."""
        if core_fields is None:
            names = self._default_core_field_names()
        else:
            names = core_fields
        lookup = {cf["name"]: cf for cf in self.CORE_FIELDS}
        return [lookup[n] for n in names if n in lookup]

    # Reporting-schema system/internal tables — never expose for model generation
    SYSTEM_TABLES = {
        "ReportColumnsMaps",          # label storage queried by _get_view_labels
        "ApplicationFormSubmissions",
        "__EFMigrationsHistory",
    }

    # --- Source type detection ---

    def _detect_source_type(self, view_name: str, db_id: int, tenant_id: str) -> str:
        """Determine which pattern applies."""
        # Block system/internal tables outright — never previewable
        if view_name in self.SYSTEM_TABLES or view_name.startswith("__"):
            return "unknown"
        if view_name.startswith("Form-"):
            return "form_view"
        # Check if it's a base name that matches a form group
        metadata = self._get_metadata(db_id, tenant_id)
        form_groups = self._group_form_versions(metadata)
        if view_name in form_groups:
            return "form_view"

        # Primary: CorrelationProvider from ReportColumnsMaps. Read from the cached
        # batch fetch (same data discover_views already pulled) rather than a per-view
        # query — this saves a round-trip per view, N-1 for combined previews.
        _, correlation_providers = self._get_report_columns_maps_info(db_id, tenant_id)
        cp = correlation_providers.get(view_name, "")
        if cp == "formversion":
            return "form_view"
        if cp == "worksheet":
            return "worksheet_view"
        if cp == "scoresheet":
            return "scoresheet_view"
        if cp:
            return "other_view"

        # Fallback: name-based heuristic
        name_lower = view_name.lower()
        if "worksheet" in name_lower:
            return "worksheet_view"
        elif "scoresheet" in name_lower:
            return "scoresheet_view"
        # Any other Reporting-schema table — generic SELECT + optional FK JOIN
        return "other_view"


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
                f"CASE\n"
                f"  WHEN {col_ref} IS NOT NULL AND TRIM({col_ref}::TEXT) <> ''\n"
                f"    THEN {col_ref}::FLOAT\n"
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

    def _columns_needing_unwrap(self, schema: str, table: str, columns: list[str],
                                  db_id: int, tenant_id: str) -> set[str]:
        """
        Return the set of column names whose stored values are JSON-array-wrapped
        (i.e. start with '["'). Detected per-column from raw (un-stripped) samples.
        """
        if not columns:
            return set()

        col_refs = ", ".join(f'"{c}"' for c in columns[:50])
        sql = f'SELECT {col_refs} FROM "{schema}"."{table}" LIMIT 5'
        try:
            result = metabase_client.execute_sql(sql, db_id, tenant_id=tenant_id)
        except Exception as e:
            logger.warning(f"Could not detect JSON unwrap for {schema}.{table}: {e}")
            return set()

        wrapped: set[str] = set()
        for row in result.get("rows", []):
            for i, col in enumerate(columns[:50]):
                if col in wrapped:
                    continue
                if i < len(row) and row[i] is not None:
                    val = str(row[i]).strip()
                    if val.startswith('["'):
                        wrapped.add(col)
        return wrapped

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

    async def _enhance_and_name(
        self, sql: str, samples: dict[str, list[str]]
    ) -> tuple[Optional[str], Optional[str], Optional[str]]:
        """One LLM call that both improves raw aliases and produces a name/description.

        Used only when the deterministic SQL still has unlabeled (raw) aliases — when
        every column already has a label we skip this and do a cheap naming-only call.
        `samples` is reused from the SQL builder, so this adds no Metabase round-trip.

        Returns (enhanced_sql, name, description); any element is None if the call or
        JSON parse fails, letting the caller fall back to deterministic SQL / derived naming.
        """
        samples_text = self._format_samples_for_prompt(samples)
        prompt = ENHANCE_AND_NAME_PROMPT.format(sql=sql, samples=samples_text)

        async with aiohttp.ClientSession() as session:
            raw = await self._post_completion(session, SYSTEM_PROMPT, prompt)

        if not raw:
            return None, None, None
        try:
            defn = self._parse_single_definition(raw)
            return defn["sql"], defn["name"], defn["description"]
        except (json.JSONDecodeError, KeyError, ValueError) as e:
            logger.warning("AI enhance+name parse failed — keeping deterministic SQL: %s", e)
            return None, None, None

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
                             tenant_id: str,
                             core_fields: Optional[list[str]] = None,
                             selected_versions: Optional[list[str]] = None) -> tuple[str, list[str]]:
        """
        Build deterministic SQL for Pattern 1 (Form Views).
        Returns (sql, column_list).
        """
        metadata = self._get_metadata(db_id, tenant_id)
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
                # Standalone form: CorrelationProvider='formversion' but the table
                # name doesn't follow the Form-<base>-V<n> pattern. Treat the table
                # itself as a single-version form.
                reporting_tables = {
                    t.get("name") for t in metadata.get("tables", [])
                    if t.get("schema") == "Reporting"
                }
                if base_name in reporting_tables:
                    versions = [base_name]
                else:
                    return "", [], {}, False, {}, False

        # Filter to selected versions if specified
        if selected_versions:
            versions = [v for v in versions if v in selected_versions]
            if not versions:
                return "", [], {}, False, {}, False

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
            # Generic FK-style ID filter (e.g. scoresheet_instance_id, assessment_id)
            if col_name.lower().endswith("_id"):
                continue
            if any(p in col_name.lower() for p in self.JUNK_PATTERNS):
                continue
            columns.append(col_name)

        if not columns:
            return "", [], {}, False

        # Detect per-column JSON wrapping (one column may be wrapped while another isn't)
        wrapped_cols = self._columns_needing_unwrap(
            "Reporting", primary_table, columns, db_id, tenant_id
        )

        # Build UNION ALL CTE parts
        union_parts = []
        for i, table_name in enumerate(versions):
            alias = f"ori{i + 1}"
            col_exprs = [f'{alias}."Id" AS "Id"', f'{alias}."ApplicationId" AS "ApplicationId"']
            for col in columns:
                if col in wrapped_cols:
                    col_exprs.append(
                        f'CASE WHEN LENGTH({alias}."{col}") > 2 '
                        f'THEN SUBSTRING({alias}."{col}", 3, LENGTH({alias}."{col}") - 4) END AS "{col}"'
                    )
                else:
                    col_exprs.append(f'{alias}."{col}" AS "{col}"')

            select_clause = ",\n".join(col_exprs)
            union_parts.append(
                f"SELECT\n{select_clause}\nFROM\n"
                f'  "Reporting"."{table_name}" AS {alias}'
            )

        combined_cte = "\n  UNION ALL\n  ".join(union_parts)

        # Get samples for type inference
        samples = self._get_column_samples("Reporting", primary_table, columns, db_id, tenant_id)

        # Resolve user-selected core fields (defaults to ReferenceNo only)
        resolved_core_fields = self._resolve_core_fields(core_fields)

        # Build final SELECT with type casts
        final_exprs = []
        for cf in resolved_core_fields:
            if cf["name"] == "ReferenceNo":
                final_exprs.append('a."ReferenceNo"')
            else:
                final_exprs.append(
                    self._type_cast_expr(f'a."{cf["name"]}"', cf["type"], cf["label"])
                )
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

        if resolved_core_fields:
            # CTE selects only the chosen core-field columns from Applications
            a_select_cols = ',\n      '.join(
                f'"public"."Applications"."{cf["name"]}"' for cf in resolved_core_fields
            )
            sql = (
                f'WITH a AS (\n'
                f'    SELECT\n      "public"."Applications"."Id",\n'
                f'      {a_select_cols}\n'
                f'    FROM\n      "public"."Applications"\n'
                f'  ),\n'
                f'  combinedOri AS ({combined_cte})\n'
                f'  SELECT\n  {final_select}\n'
                f'  FROM\n  combinedOri\n'
                f'  LEFT JOIN a ON a."Id" = combinedOri."ApplicationId"'
            )
        else:
            # No core fields selected → skip the Applications JOIN entirely
            sql = (
                f'WITH combinedOri AS ({combined_cte})\n'
                f'  SELECT\n  {final_select}\n'
                f'  FROM\n  combinedOri'
            )

        column_labels = [
            "ReferenceNo" if cf["name"] == "ReferenceNo" else cf["label"]
            for cf in resolved_core_fields
        ] + [labels.get(c, {}).get("label", c) for c in columns]

        # Any column without a real label falls back to its raw name → worth an AI
        # alias pass. When every column is labeled the deterministic SQL is already clean.
        needs_enhancement = any(not labels.get(c, {}).get("label") for c in columns)
        return sql, column_labels, samples, needs_enhancement

    def _build_worksheet_view_sql(self, view_name: str, db_id: int,
                                   tenant_id: str,
                                   core_fields: Optional[list[str]] = None) -> tuple[str, list[str]]:
        """
        Build deterministic SQL for worksheet/scoresheet views.
        Simpler than form views: no UNION ALL, no JSON unwrap.
        Just SELECT columns with optional JOIN to Applications for core fields.
        Returns (sql, column_list).
        """
        metadata = self._get_metadata(db_id, tenant_id)

        # Find the view
        target_fields = []
        for table in metadata.get("tables", []):
            if table.get("name") == view_name and table.get("schema") == "Reporting":
                target_fields = table.get("fields", [])
                break

        if not target_fields:
            return "", [], {}, False

        # Get labels
        labels = self._get_view_labels(view_name, db_id, tenant_id)
        if not labels:
            custom_labels = self._get_custom_field_labels(db_id, tenant_id)
        else:
            custom_labels = {}

        # Detect FK column for JOIN (check both PascalCase and snake_case)
        fk_column = None
        field_names = {f.get("name", "") for f in target_fields}
        for candidate in ("ApplicationId", "application_id", "CorrelationId", "correlation_id"):
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
            # Generic FK-style ID filter (e.g. scoresheet_instance_id, assessment_id)
            if col_name.lower().endswith("_id"):
                continue
            if any(p in col_name.lower() for p in self.JUNK_PATTERNS):
                continue
            columns.append(col_name)

        if not columns:
            return "", [], {}, False

        # Get samples for type inference
        samples = self._get_column_samples("Reporting", view_name, columns, db_id, tenant_id)

        # Resolve user-selected core fields (defaults to ReferenceNo only)
        resolved_core_fields = self._resolve_core_fields(core_fields) if join_condition else []

        # Build SELECT expressions
        select_exprs = []
        for cf in resolved_core_fields:
            if cf["name"] == "ReferenceNo":
                # Keep the historical un-cast form for ReferenceNo to preserve
                # the existing column header and avoid unnecessary CASE wrappers.
                select_exprs.append('a."ReferenceNo"')
            else:
                select_exprs.append(
                    self._type_cast_expr(f'a."{cf["name"]}"', cf["type"], cf["label"])
                )

        for col in columns:
            label_info = labels.get(col, {})
            label = label_info.get("label", "") or custom_labels.get(col, "") or col
            forms_type = label_info.get("forms_type", "")
            if not forms_type or forms_type == "text":
                forms_type = self._infer_column_type(col, samples.get(col, []))
            col_ref = f'"t"."{col}"'
            select_exprs.append(self._type_cast_expr(col_ref, forms_type, label))

        select_clause = ",\n  ".join(select_exprs)

        # Only emit the JOIN when we actually need core fields from Applications
        if join_condition and resolved_core_fields:
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

        column_labels = [cf["label"] if cf["name"] != "ReferenceNo" else "ReferenceNo"
                         for cf in resolved_core_fields]
        needs_enhancement = False
        for col in columns:
            label_info = labels.get(col, {})
            real_label = label_info.get("label", "") or custom_labels.get(col, "")
            column_labels.append(real_label or col)
            if not real_label:
                needs_enhancement = True

        return sql, column_labels, samples, needs_enhancement

    # --- AI naming helper ---

    async def _generate_name_description(self, source: str,
                                          columns: list[str]) -> tuple[str, str]:
        """Use AI only for naming and description (small token budget).

        NAMING_PROMPT returns just {name, description} — parsed directly here rather
        than via _parse_single_definition (which also requires a `sql` field).
        Falls back to a name derived from the source view when AI is unavailable.
        """
        columns_preview = ", ".join(columns[:25])
        prompt = NAMING_PROMPT.format(source=source, columns=columns_preview)

        async with aiohttp.ClientSession() as session:
            raw = await self._post_completion(session, SYSTEM_PROMPT, prompt, max_tokens=300)

        if raw:
            try:
                text = re.sub(r"^```(?:json)?\s*", "", raw.strip(), flags=re.IGNORECASE)
                text = re.sub(r"\s*```$", "", text.strip())
                start, end = text.find("{"), text.rfind("}")
                if start != -1 and end > start:
                    defn = json.loads(text[start:end + 1])
                    name = (defn.get("name") or "").strip()
                    description = (defn.get("description") or "").strip()
                    if name and description:
                        return name, description
            except (json.JSONDecodeError, ValueError):
                pass

        return self._fallback_name_description(source)

    def _fallback_name_description(self, source: str) -> tuple[str, str]:
        """Derive a name/description from the source view name when AI is unavailable."""
        clean_name = re.sub(r'-V\d+$', '', source).replace("-", " ").strip()
        clean_name = re.sub(r'^Form-\s*', '', clean_name)
        return clean_name, f"Data model from {source}"

    def discover_views(self, db_id: int, tenant_id: str) -> list[dict]:
        """
        Step 0: Discover available Reporting schema views (Worksheet, Scoresheet, Form).

        Form views are grouped by base name — selecting a group runs UNION ALL across
        all its versions, which is what the SQL builder does anyway.

        All non-system Reporting views are returned; views with no rows are tagged
        is_empty=True so the UI can label them rather than hide them.

        Returns list of {view_name, display_name, column_count, has_labels,
        source_type, is_empty, ...}.
        """
        metadata = self._get_metadata(db_id, tenant_id)
        tables_by_name = {
            t.get("name"): t for t in metadata.get("tables", [])
            if t.get("schema") == "Reporting"
        }

        views_with_labels, correlation_providers = self._get_report_columns_maps_info(db_id, tenant_id)

        # Regex-based multi-version form grouping (Form-<base>-V<n>).
        form_groups = self._group_form_versions(metadata)

        # Physical tables already captured by a regex form group.
        grouped_tables: set[str] = set()
        primary_for_group: dict[str, str] = {}
        for base_name, version_tables in form_groups.items():
            grouped_tables.update(version_tables)
            if version_tables and version_tables[0] in tables_by_name:
                primary_for_group[base_name] = version_tables[0]

        # Every other non-system Reporting table — classified by CorrelationProvider.
        standalone_names: list[str] = []
        for name in tables_by_name:
            if not name or name in grouped_tables:
                continue
            if name in self.SYSTEM_TABLES or name.startswith("__"):
                continue
            standalone_names.append(name)

        # Two batched round-trips replace the old 2×N per-view probes.
        physical_names = list(primary_for_group.values()) + standalone_names
        has_data_map = self._batch_check_view_data(physical_names, db_id, tenant_id)

        views: list[dict] = []

        for base_name, primary in primary_for_group.items():
            primary_table = tables_by_name[primary]
            column_count = sum(
                1 for f in primary_table.get("fields", [])
                if f.get("name") not in self.JUNK_COLUMNS
            )
            version_count = len(form_groups[base_name])
            version_label = f"{version_count} version{'s' if version_count != 1 else ''}"

            # Build per-version detail list for the frontend version picker
            version_details = []
            for table_name in form_groups[base_name]:
                match = FORM_VERSION_RE.match(table_name)
                v_num = int(match.group(2)) if match else 0
                version_details.append({"table_name": table_name, "version": v_num})

            views.append({
                "view_name": base_name,
                "display_name": base_name,
                "column_count": column_count,
                "has_labels": primary in views_with_labels,
                "source_type": "form_view",
                "form_group": base_name,
                "version": version_label,
                "versions": version_details,
                "is_empty": not has_data_map.get(primary, True),
            })

        for name in standalone_names:
            table = tables_by_name[name]

            # Primary: CorrelationProvider from ReportColumnsMaps
            cp = correlation_providers.get(name, "")
            if cp == "formversion":
                source_type = "form_view"
            elif cp == "worksheet":
                source_type = "worksheet_view"
            elif cp == "scoresheet":
                source_type = "scoresheet_view"
            elif cp:
                source_type = "other_view"
            else:
                # Fallback: name-based heuristic for views not in ReportColumnsMaps
                name_lower = name.lower()
                if "worksheet" in name_lower:
                    source_type = "worksheet_view"
                elif "scoresheet" in name_lower:
                    source_type = "scoresheet_view"
                elif name.startswith("Form-"):
                    source_type = "form_view"
                else:
                    source_type = "other_view"

            column_count = sum(
                1 for f in table.get("fields", [])
                if f.get("name") not in self.JUNK_COLUMNS
            )

            view_entry = {
                "view_name": name,
                "display_name": name,
                "column_count": column_count,
                "has_labels": name in views_with_labels,
                "source_type": source_type,
                "is_empty": not has_data_map.get(name, True),
            }

            # A formversion view that didn't match the Form-<base>-V<n> regex is a
            # standalone, single-version form — give it the same shape the UI expects.
            if source_type == "form_view":
                match = FORM_VERSION_RE.match(name)
                v_num = int(match.group(2)) if match else 0
                view_entry["form_group"] = name
                view_entry["version"] = "1 version"
                view_entry["versions"] = [{"table_name": name, "version": v_num}]

            views.append(view_entry)

        views.sort(key=lambda v: (v["source_type"], v["view_name"]))
        logger.info(f"Discovered {len(views)} views for tenant {tenant_id}")
        return views

    async def _validate_sql_with_heal(
        self, sql: str, db_id: int, tenant_id: str,
        heal_args: Optional[tuple[str, str, str]] = None,
    ) -> tuple[str, bool, Optional[str], Optional[dict]]:
        """Run SQL once (bounded) to get validity AND preview rows in one round-trip.

        Replaces the old validate_sql()-then-execute_sql() pattern: validation no
        longer pulls the full result set, and the API layer reuses the returned rows
        instead of executing the query a second time. On failure, self-heals once and
        re-runs. heal_args = (columns_text, join_keys_text, relationships_text); pass
        None to skip self-healing. Returns (final_sql, is_valid, error, preview_data).
        """
        limit = config.app.data_model_preview_row_limit
        is_valid, error, data = metabase_client.run_query_checked(
            sql, db_id, tenant_id=tenant_id, max_rows=limit
        )
        if not is_valid and heal_args is not None:
            healed = await self._self_heal(sql, error or "", *heal_args)
            if healed:
                h_valid, h_error, h_data = metabase_client.run_query_checked(
                    healed, db_id, tenant_id=tenant_id, max_rows=limit
                )
                if h_valid:
                    return healed, True, h_error, h_data
        return sql, is_valid, error, data

    async def preview_model(self, view_name: str, db_id: int,
                            tenant_id: str,
                            core_fields: Optional[list[str]] = None,
                            selected_versions: Optional[list[str]] = None) -> dict:
        """
        Step 1: Generate a model proposal for a selected view.

        Routes to deterministic builders for known patterns, falls back to AI.
        """
        source_type = self._detect_source_type(view_name, db_id, tenant_id)

        if source_type == "form_view":
            # Extract base name from full table name if needed
            match = FORM_VERSION_RE.match(view_name)
            base_name = match.group(1) if match else view_name
            sql, columns, samples, needs_enhancement = self._build_form_view_sql(
                base_name, db_id, tenant_id, core_fields, selected_versions
            )
        elif source_type in ("worksheet_view", "scoresheet_view", "other_view"):
            # other_view: any non-Form/non-worksheet/non-scoresheet Reporting table.
            # The worksheet builder is generic — simple SELECT + optional FK JOIN —
            # and degrades gracefully when no FK is found.
            sql, columns, samples, needs_enhancement = self._build_worksheet_view_sql(
                view_name, db_id, tenant_id, core_fields
            )
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

        # One LLM call per preview. When every column already has a label the
        # deterministic SQL is clean → a cheap naming-only call. Otherwise a single
        # combined call both improves the raw aliases and returns name/description,
        # reusing the samples the builder already fetched (no extra round-trip).
        name = description = None
        if needs_enhancement:
            try:
                enhanced_sql, name, description = await self._enhance_and_name(sql, samples)
            except Exception as e:
                logger.warning("AI enhance+name failed — keeping deterministic SQL: %s", e)
                enhanced_sql = None
            if enhanced_sql and self._validate_enhancement(sql, enhanced_sql):
                sql = enhanced_sql
        else:
            try:
                name, description = await self._generate_name_description(view_name, columns)
            except Exception as e:
                logger.warning("AI naming failed — using fallback name: %s", e)

        if not name or not description:
            name, description = self._fallback_name_description(view_name)

        # Single bounded execution validates the SQL and yields the preview rows.
        sql, is_valid, error, preview_raw = await self._validate_sql_with_heal(
            sql, db_id, tenant_id,
            heal_args=("\n".join(columns), "(deterministic)", "(deterministic)"),
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
        if preview_raw is not None:
            proposal["_preview_raw"] = preview_raw

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

        # Single bounded execution validates the SQL and yields the preview rows.
        sql, is_valid, error, preview_raw = await self._validate_sql_with_heal(
            definition["sql"], db_id, tenant_id,
            heal_args=(columns_text, join_keys_text, relationships_text),
        )
        definition["sql"] = sql

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
        if preview_raw is not None:
            proposal["_preview_raw"] = preview_raw

        logger.info(
            "Model preview complete - tenant=%s view=%s valid=%s",
            tenant_id, view_name, is_valid
        )
        return proposal

    async def preview_combined_model(self, view_names: list[str], db_id: int,
                                     tenant_id: str,
                                     core_fields: Optional[list[str]] = None) -> dict:
        """
        Generate a single merged model SQL from 2+ views joined via "ReferenceNo".

        Each view's SQL is wrapped as a named CTE (PostgreSQL supports nested CTEs
        inside CTE bodies). Secondary views are LEFT JOINed to the primary view
        on "ReferenceNo". Column aliases are deduplicated.
        """
        # ReferenceNo is the JOIN key — auto-prepend if caller omitted it
        if core_fields is None:
            effective_core_fields = self._default_core_field_names()
        else:
            effective_core_fields = list(core_fields)
            if "ReferenceNo" not in effective_core_fields:
                effective_core_fields.insert(0, "ReferenceNo")

        if len(view_names) < 2:
            return await self.preview_model(view_names[0], db_id, tenant_id, effective_core_fields)

        # Build individual SQL for each view: [(view_name, sql, column_labels), ...]
        built: list[tuple[str, str, list[str]]] = []
        for vn in view_names:
            source_type = self._detect_source_type(vn, db_id, tenant_id)
            if source_type == "form_view":
                match = FORM_VERSION_RE.match(vn)
                base_name = match.group(1) if match else vn
                sql, cols, _, _ = self._build_form_view_sql(base_name, db_id, tenant_id, effective_core_fields)
            else:
                # worksheet_view, scoresheet_view, other_view — worksheet builder handles all
                sql, cols, _, _ = self._build_worksheet_view_sql(vn, db_id, tenant_id, effective_core_fields)

            if not sql:
                logger.warning("Could not build SQL for '%s' in combined model — skipping", vn)
                continue
            built.append((vn, sql, cols))

        if len(built) < 2:
            if built:
                return await self.preview_model(built[0][0], db_id, tenant_id, effective_core_fields)
            return {
                "name": "Combined Model",
                "description": "Could not build combined model SQL",
                "sql": "",
                "valid": False,
                "error": "No valid SQL could be built for the selected views",
                "source_view": " + ".join(view_names),
                "columns": [],
                "excluded_columns": [],
            }

        # Primary must have "ReferenceNo" to act as the join anchor
        primary_vn, _, primary_cols = built[0]
        primary_has_ref = "referenceno" in {c.lower() for c in primary_cols}
        if not primary_has_ref:
            return {
                "name": "Combined Model",
                "description": "Could not build combined model SQL",
                "sql": "",
                "valid": False,
                "error": (
                    f"Primary view '{primary_vn}' has no \"ReferenceNo\" column to join on. "
                    f"Combine requires views that link to public.Applications."
                ),
                "source_view": " + ".join(view_names),
                "columns": [],
                "excluded_columns": [],
            }

        # Filter secondaries to those that share the join key — never CROSS JOIN
        joinable = [built[0]]
        skipped: list[str] = []
        for entry in built[1:]:
            vn, _, cols = entry
            if "referenceno" in {c.lower() for c in cols}:
                joinable.append(entry)
            else:
                skipped.append(vn)
                logger.warning(
                    "Combined model: skipping '%s' — no \"ReferenceNo\" column to join on", vn
                )

        if len(joinable) < 2:
            return {
                "name": "Combined Model",
                "description": "Could not build combined model SQL",
                "sql": "",
                "valid": False,
                "error": (
                    f"None of the secondary views share a \"ReferenceNo\" join key with "
                    f"'{primary_vn}'. Skipped: {', '.join(skipped)}."
                ),
                "source_view": " + ".join(view_names),
                "columns": [],
                "excluded_columns": [],
            }

        # Wrap each as a named CTE
        cte_parts = [
            f"view_{i + 1} AS (\n{sql}\n)"
            for i, (_, sql, _) in enumerate(joinable)
        ]

        # Build outer SELECT — deduplicate aliases across views
        used_lower: set[str] = {c.lower() for c in primary_cols}
        select_exprs = [f'view_1."{col}"' for col in primary_cols]

        for i, (_, _, cols) in enumerate(joinable[1:], start=2):
            alias = f"view_{i}"
            for col in cols:
                if col.lower() not in used_lower:
                    select_exprs.append(f'{alias}."{col}"')
                    used_lower.add(col.lower())

        # JOIN secondary CTEs to primary via "ReferenceNo"
        join_clauses = [
            f'LEFT JOIN view_{i} ON view_{i}."ReferenceNo" = view_1."ReferenceNo"'
            for i in range(2, len(joinable) + 1)
        ]

        combined_sql = (
            f"WITH\n{',\n'.join(cte_parts)}\n"
            f"SELECT\n  {',\n  '.join(select_exprs)}\n"
            f"FROM view_1\n"
            + "\n".join(join_clauses)
        )

        # Single bounded execution validates the SQL and yields the preview rows.
        combined_sql, is_valid, error, preview_raw = await self._validate_sql_with_heal(
            combined_sql, db_id, tenant_id, heal_args=None
        )

        # AI naming based on combined column set (only views actually joined)
        all_cols = list({c for _, _, cols in joinable for c in cols})
        source_label = " + ".join(vn for vn, _, _ in joinable)
        name, description = await self._generate_name_description(source_label, all_cols)

        logger.info(
            "Combined model preview complete - tenant=%s views=%s valid=%s",
            tenant_id, view_names, is_valid
        )
        if skipped:
            note = f" (skipped — no \"ReferenceNo\" join key: {', '.join(skipped)})"
            description = (description or "") + note

        proposal = {
            "name": name,
            "description": description,
            "sql": combined_sql,
            "valid": is_valid,
            "error": error,
            "source_view": " + ".join(vn for vn, _, _ in joinable),
            "columns": all_cols,
            "excluded_columns": [],
        }
        if preview_raw is not None:
            proposal["_preview_raw"] = preview_raw
        return proposal

    # ----- Discover & Modify Existing Models -----

    def discover_existing_models(self, collection_id: int, tenant_id: str) -> list[dict]:
        """Return lightweight list of existing model cards: [{card_id, name, description}]."""
        cards = metabase_client.get_collection_models(collection_id, tenant_id)
        return [
            {
                "card_id": c["id"],
                "name": c.get("name", ""),
                "description": c.get("description") or "",
            }
            for c in cards
        ]

    async def preview_model_modification(
        self, card_id: int, prompt: str, additional_view_names: list[str],
        db_id: int, collection_id: int, tenant_id: str,
        core_fields: Optional[list[str]] = None
    ) -> dict:
        """
        Generate a modified variant of an existing model.
        At least one of prompt, additional_view_names, or core_fields must be non-empty.
        Returns a ModelProposal dict (same shape as preview_model).
        """
        # core_fields is None → not provided; list (even empty) → user-provided intent
        if not prompt and not additional_view_names and core_fields is None:
            raise ValueError(
                "At least one of prompt, additional_view_names, or core_fields is required"
            )

        # 1. Fetch existing card
        card = metabase_client.get_card(card_id, tenant_id)
        dataset_query = card.get("dataset_query", {})
        current_name = card.get("name", "Unnamed Model")

        logger.debug(f"Card {card_id} dataset_query keys: {list(dataset_query.keys())}, type={dataset_query.get('type')}, database={dataset_query.get('database')}")

        query_type = dataset_query.get("type", "unknown")
        if query_type == "native":
            current_sql = dataset_query.get("native", {}).get("query", "")
        else:
            logger.info(f"Model card_id={card_id} uses structured query (type='{query_type}'), converting to native SQL")
            current_sql = metabase_client.get_native_query(dataset_query, tenant_id, db_id=db_id) or ""

        if not current_sql:
            raise ValueError(
                f"Could not extract SQL from existing model (query type='{query_type}'). "
                "Metabase may not support converting this structured query to native SQL."
            )

        # Extract current columns from SQL (AS "..." pattern)
        current_columns = re.findall(r'AS\s+"([^"]+)"', current_sql)
        if not current_columns:
            # Fallback: try to get column names from card result_metadata
            current_columns = [
                col.get("display_name", col.get("name", ""))
                for col in card.get("result_metadata", [])
            ]

        # 2. Build additional view SQL if needed (each builder returns (sql, columns))
        # Additional view CTEs must include ReferenceNo as the JOIN key, regardless
        # of the user's core_fields choice. The AI prompt still receives the literal
        # user selection — this adjustment is only for the internal view-building plumbing.
        if additional_view_names:
            if not core_fields:  # None or []
                builder_core_fields = self._default_core_field_names()
            else:
                builder_core_fields = list(core_fields)
                if "ReferenceNo" not in builder_core_fields:
                    builder_core_fields.insert(0, "ReferenceNo")
        else:
            builder_core_fields = core_fields

        additional_views_text = "(none)"
        if additional_view_names:
            view_sqls: list[str] = []
            for vn in additional_view_names:
                source_type = self._detect_source_type(vn, db_id, tenant_id)
                if source_type == "form_view":
                    # _build_form_view_sql expects the base name (e.g. "GrantApplication"),
                    # not the full table name "Form-GrantApplication-V2"
                    match = FORM_VERSION_RE.match(vn)
                    base_name = match.group(1) if match else vn
                    view_sql, _, _, _ = self._build_form_view_sql(
                        base_name, db_id, tenant_id, builder_core_fields
                    )
                elif source_type in ("worksheet_view", "scoresheet_view", "other_view"):
                    view_sql, _, _, _ = self._build_worksheet_view_sql(
                        vn, db_id, tenant_id, builder_core_fields
                    )
                else:
                    logger.warning("Skipping unknown view '%s' in modification", vn)
                    continue
                if view_sql:
                    view_sqls.append(f"-- View: {vn}\n{view_sql}")
            if view_sqls:
                additional_views_text = "\n\n".join(view_sqls)

        # 2b. Format core fields for the prompt
        resolved_core_fields = self._resolve_core_fields(core_fields) if core_fields else []
        if resolved_core_fields:
            # ReferenceNo uses its column name as the header (no rename) to match the
            # deterministic builder's output. Other core fields use their label as alias.
            def _alias_for(cf):
                return cf["name"] if cf["name"] == "ReferenceNo" else cf["label"]
            core_fields_text = "\n".join(
                f'  "{cf["name"]}" → alias "{_alias_for(cf)}" (type: {cf["type"]})'
                for cf in resolved_core_fields
            )
        else:
            core_fields_text = "(none)"

        # 3. Call AI to rewrite
        user_prompt = prompt.strip() if prompt else "(none)"
        modify_request = MODIFY_PROMPT.format(
            current_sql=current_sql,
            current_columns=", ".join(current_columns) if current_columns else "(unknown)",
            user_prompt=user_prompt,
            core_fields_text=core_fields_text,
            additional_views_text=additional_views_text,
        )

        async with aiohttp.ClientSession() as session:
            new_sql = await self._post_completion(session, SYSTEM_PROMPT, modify_request)
        if not new_sql:
            raise RuntimeError("AI service failed to generate modified SQL — the LLM request timed out or returned an error")

        # Clean markdown fences
        new_sql = re.sub(r"^```(?:sql)?\s*", "", new_sql.strip(), flags=re.IGNORECASE)
        new_sql = re.sub(r"\s*```$", "", new_sql.strip())

        # 4. Single bounded execution validates the SQL, self-heals once, and yields rows.
        columns_text = "\n".join(f"  {c}" for c in current_columns)
        new_sql, is_valid, error, preview_raw = await self._validate_sql_with_heal(
            new_sql, db_id, tenant_id,
            heal_args=(
                columns_text,
                "(modification — preserve existing JOIN structure)",
                "(modification — preserve existing relationships)",
            ),
        )

        # 5. Generate name (variant)
        new_name = f"{current_name} v2"
        # Check for existing vN names
        existing_names = metabase_client.get_all_card_names(tenant_id)
        if new_name in existing_names:
            for n in range(3, 100):
                candidate = f"{current_name} v{n}"
                if candidate not in existing_names:
                    new_name = candidate
                    break

        # Extract new columns
        new_columns = re.findall(r'AS\s+"([^"]+)"', new_sql)

        description = f"Modified variant of \"{current_name}\""
        if prompt:
            description += f" — {prompt[:100]}"

        proposal = {
            "name": new_name,
            "description": description,
            "sql": new_sql,
            "valid": is_valid,
            "error": error,
            "source_view": current_name,
            "columns": new_columns,
            "excluded_columns": [],
        }
        if preview_raw is not None:
            proposal["_preview_raw"] = preview_raw
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
        metadata = self._get_metadata(db_id, tenant_id)

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

    _BATCH_DATA_CHECK_SIZE = 100

    def _batch_check_view_data(
        self, view_names: list[str], db_id: int, tenant_id: str
    ) -> dict[str, bool]:
        """One round-trip EXISTS probe across many Reporting views.

        Replaces N sequential per-view existence checks. Falls back to assuming each
        view has data if the batch fails — better to show a stale empty-flag than
        block discovery.
        """
        if not view_names:
            return {}

        # Names come from Metabase metadata, not user input, but escape defensively.
        def quote_ident(n: str) -> str:
            return '"' + n.replace('"', '""') + '"'

        def quote_lit(n: str) -> str:
            return "'" + n.replace("'", "''") + "'"

        result_map: dict[str, bool] = {}
        for i in range(0, len(view_names), self._BATCH_DATA_CHECK_SIZE):
            chunk = view_names[i:i + self._BATCH_DATA_CHECK_SIZE]
            parts = [
                f'SELECT {quote_lit(name)} AS view_name, '
                f'EXISTS (SELECT 1 FROM "Reporting".{quote_ident(name)}) AS has_data'
                for name in chunk
            ]
            sql = "\nUNION ALL\n".join(parts)
            try:
                result = metabase_client.execute_sql(sql, db_id, tenant_id=tenant_id)
                for row in result.get("rows", []):
                    if row and len(row) >= 2:
                        result_map[row[0]] = bool(row[1])
            except Exception as e:
                logger.warning(
                    "discover_views: batched data check failed for chunk starting at %d "
                    "(%s); assuming has_data=True for these",
                    i, e,
                )
                for name in chunk:
                    result_map.setdefault(name, True)

        return result_map

    def _get_report_columns_maps_info(
        self, db_id: int, tenant_id: str
    ) -> tuple[set[str], dict[str, str]]:
        """Fetch (views_with_labels, correlation_providers) from ReportColumnsMaps.

        One round-trip, cached per (db_id, tenant_id) for the same short TTL as
        metadata. Source-type detection reads this once per view, so caching collapses
        those repeated lookups to a single query within a preview.

        - views_with_labels: set of ViewName values (for the has_labels flag)
        - correlation_providers: {ViewName: lowercase CorrelationProvider}
        """
        key = (int(db_id), str(tenant_id).strip())
        now = time.monotonic()
        with self._rcm_lock:
            cached = self._rcm_cache.get(key)
            if cached and (now - cached[0]) < self._META_CACHE_TTL:
                return cached[1]

            sql = 'SELECT DISTINCT "ViewName", "CorrelationProvider" FROM "Reporting"."ReportColumnsMaps"'
            views_with_labels: set[str] = set()
            correlation_providers: dict[str, str] = {}
            try:
                result = metabase_client.execute_sql(sql, db_id, tenant_id=tenant_id)
                for row in result.get("rows", []):
                    if row and row[0]:
                        views_with_labels.add(row[0])
                        if len(row) > 1 and row[1]:
                            correlation_providers[row[0]] = row[1].strip().lower()
            except Exception as e:
                logger.warning(
                    "discover_views: ReportColumnsMaps query failed (%s); "
                    "assuming no labels and no correlation providers",
                    e,
                )

            info = (views_with_labels, correlation_providers)
            self._rcm_cache[key] = (now, info)
            return info

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
        """Check if the view has an application ID column by querying it directly."""
        for col_name in ("ApplicationId", "application_id"):
            sql = f'SELECT "{col_name}" FROM "{schema}"."{view_name}" LIMIT 1'
            try:
                result = metabase_client.execute_sql(sql, db_id, tenant_id=tenant_id)
                if "error" not in result:
                    logger.info(f"Found {col_name} column in {schema}.{view_name}")
                    return col_name
            except Exception as e:
                logger.debug(f"{col_name} not found in {schema}.{view_name}: {e}")
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
        metadata = self._get_metadata(db_id, tenant_id)

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
                               system_message: str, prompt: str,
                               max_tokens: int = 4000) -> Optional[str]:
        """Call the configured LLM. `max_tokens` caps completion length — naming-only
        calls pass a small value to avoid paying for the full SQL-sized budget."""
        ai_cfg = config.ai

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
            "max_completion_tokens": max_tokens,
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
