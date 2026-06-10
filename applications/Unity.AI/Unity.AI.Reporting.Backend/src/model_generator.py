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
from model_prompts import (
    ENHANCE_AND_NAME_PROMPT,
    MODEL_GENERATION_PROMPT,
    MODIFY_PROMPT,
    NAMING_PROMPT,
    SELF_HEAL_PROMPT,
    SYSTEM_PROMPT,
)
from schema_repository import SchemaRepository

CONTENT_TYPE = "application/json"

logger = logging.getLogger(__name__)

# Regex for parsing form view table names: "Form-<base>-V<n>" or
# "Form-<base> Alternate-V<n>". <base> uses \S+ (form names have no internal
# whitespace) so it stays disjoint from the optional \s+ Alternate group; this
# removes the ambiguity a .+? caused and avoids polynomial backtracking on
# untrusted table names (CodeQL py/polynomial-redos).
FORM_VERSION_RE = re.compile(r'^Form-(\S+)(?:\s+Alternate)?-V(\d+)$')


def _strip_trailing_code_fence(text: str) -> str:
    """Strip a trailing ``` markdown fence (and surrounding whitespace) from an
    LLM response.

    Done with string operations instead of a ``\\s*```$`` regex: that pattern
    has polynomial backtracking on untrusted model output, because re.sub
    retries the unanchored ``\\s*`` at every position of a whitespace run
    (python:S5852 / CWE-1333). String slicing is linear-time.
    """
    text = text.strip()
    if text.endswith("```"):
        text = text[:-3].rstrip()
    return text


class DataModelGenerator:
    """Hybrid AI-assisted data model generator using Reporting views."""

    def __init__(self) -> None:
        # All cached, read-only Metabase access (metadata, labels, samples, FK probes)
        # lives in SchemaRepository so this class stays focused on SQL/AI orchestration.
        self.schema = SchemaRepository()

    JUNK_COLUMNS = {
        "Id", "CreatorId", "LastModificationTime",
        "LastModifierId", "ExtraProperties", "ConcurrencyStamp",
        "CreationTime", "CorrelationProvider",
        "WorksheetId", "WorksheetCorrelationId",
    }
    # CorrelationId intentionally NOT in JUNK_COLUMNS — used as FK for JOINs
    JUNK_PATTERNS = {"password", "ssn", "sin", "secret", "token"}

    COMBINED_MODEL_NAME = "Combined Model"
    COMBINED_MODEL_BUILD_FAILED_DESC = "Could not build combined model SQL"

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
        metadata = self.schema.get_metadata(db_id, tenant_id)
        form_groups = self._group_form_versions(metadata)
        if view_name in form_groups:
            return "form_view"

        # Primary: CorrelationProvider from ReportColumnsMaps. Read from the cached
        # batch fetch (same data discover_views already pulled) rather than a per-view
        # query — this saves a round-trip per view, N-1 for combined previews.
        _, correlation_providers = self.schema.get_report_columns_maps_info(db_id, tenant_id)
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

    @staticmethod
    def _extract_from_clause(sql: str) -> str:
        match = re.search(r'\bFROM\b(.+)$', sql, flags=re.IGNORECASE | re.DOTALL)
        return re.sub(r'\s+', ' ', match.group(1).strip()) if match else ""

    @staticmethod
    def _extract_cte(sql: str) -> str:
        parts = re.split(r'\)\s*SELECT\b', sql, flags=re.IGNORECASE)
        return parts[0] if len(parts) >= 2 else ""

    @staticmethod
    def _count_select_cols(sql: str, has_cte: bool) -> int:
        if has_cte:
            parts = re.split(r'\)\s*SELECT\b', sql, flags=re.IGNORECASE)
            if len(parts) < 2:
                return 0
            final = parts[-1].split("FROM")[0] if "FROM" in parts[-1] else parts[-1]
        else:
            select_match = re.search(r'\bSELECT\b(.+?)\bFROM\b', sql, flags=re.IGNORECASE | re.DOTALL)
            if not select_match:
                return 0
            final = select_match.group(1)
        cleaned = re.sub(r'\bCASE\b.*?\bEND\b', 'X', final, flags=re.IGNORECASE | re.DOTALL)
        return cleaned.count(",") + 1

    def _cte_structure_preserved(self, original_sql: str, enhanced_sql: str) -> bool:
        original_cte = self._extract_cte(original_sql)
        enhanced_cte = self._extract_cte(enhanced_sql)
        if not original_cte or not enhanced_cte:
            return False
        norm_orig = re.sub(r'\s+', ' ', original_cte.strip())
        norm_enh = re.sub(r'\s+', ' ', enhanced_cte.strip())
        if norm_orig != norm_enh:
            logger.warning("AI enhancement modified CTE structure — rejecting")
            return False
        return True

    def _from_clause_preserved(self, original_sql: str, enhanced_sql: str) -> bool:
        if self._extract_from_clause(original_sql) != self._extract_from_clause(enhanced_sql):
            logger.warning("AI enhancement modified FROM clause — rejecting")
            return False
        return True

    def _validate_enhancement(self, original_sql: str, enhanced_sql: str) -> bool:
        """Verify AI didn't modify structure — only aliases/casts in SELECT."""
        has_cte = original_sql.strip().upper().startswith("WITH")

        structure_ok = (
            self._cte_structure_preserved(original_sql, enhanced_sql) if has_cte
            else self._from_clause_preserved(original_sql, enhanced_sql)
        )
        if not structure_ok:
            return False

        orig_count = self._count_select_cols(original_sql, has_cte)
        enh_count = self._count_select_cols(enhanced_sql, has_cte)
        if enh_count < orig_count:
            logger.warning(
                "AI enhancement removed columns (%d → %d) — rejecting",
                orig_count, enh_count
            )
            return False

        return True

    # --- Deterministic SQL builders ---

    def _resolve_form_versions(self, base_name: str, metadata: dict,
                                all_groups: dict) -> tuple[Optional[list[str]], str]:
        """Resolve a base_name to its list of form-version tables.

        Tries the group key, then a full-table-name match, then a standalone
        Reporting table fallback. Returns (None, base_name) if nothing matches.
        """
        versions = all_groups.get(base_name, [])
        if versions:
            return versions, base_name

        for base, tables in all_groups.items():
            if base_name in tables:
                return tables, base

        reporting_tables = {
            t.get("name") for t in metadata.get("tables", [])
            if t.get("schema") == "Reporting"
        }
        if base_name in reporting_tables:
            return [base_name], base_name
        return None, base_name

    def _get_reporting_table_fields(self, table_name: str, metadata: dict) -> list[dict]:
        for table in metadata.get("tables", []):
            if table.get("name") == table_name and table.get("schema") == "Reporting":
                return table.get("fields", [])
        return []

    def _select_form_columns(self, primary_fields: list[dict]) -> list[str]:
        """Filter out junk/FK-style columns from a form view's fields."""
        columns = []
        for field in primary_fields:
            col_name = field.get("name", "")
            if not col_name or col_name in self.JUNK_COLUMNS or col_name == "ApplicationId":
                continue
            if col_name.lower().endswith("_id"):
                continue
            if any(p in col_name.lower() for p in self.JUNK_PATTERNS):
                continue
            columns.append(col_name)
        return columns

    def _build_union_cte(self, versions: list[str], columns: list[str],
                          wrapped_cols: set) -> str:
        """Build the UNION ALL body that combines all form-version tables."""
        union_parts = []
        for i, table_name in enumerate(versions):
            alias = f"ori{i + 1}"
            col_exprs = [f'{alias}."Id" AS "Id"', f'{alias}."ApplicationId" AS "ApplicationId"']
            for col in columns:
                col_exprs.append(self._form_column_expr(alias, col, col in wrapped_cols))
            select_clause = ",\n".join(col_exprs)
            union_parts.append(
                f"SELECT\n{select_clause}\nFROM\n"
                f'  "Reporting"."{table_name}" AS {alias}'
            )
        return "\n  UNION ALL\n  ".join(union_parts)

    @staticmethod
    def _form_column_expr(alias: str, col: str, wrapped: bool) -> str:
        if wrapped:
            return (
                f'CASE WHEN LENGTH({alias}."{col}") > 2 '
                f'THEN SUBSTRING({alias}."{col}", 3, LENGTH({alias}."{col}") - 4) END AS "{col}"'
            )
        return f'{alias}."{col}" AS "{col}"'

    def _build_form_select_exprs(self, resolved_core_fields: list[dict], columns: list[str],
                                   labels: dict, samples: dict) -> list[str]:
        """Build SELECT expressions for core fields + form columns with type casts."""
        exprs = []
        for cf in resolved_core_fields:
            if cf["name"] == "ReferenceNo":
                exprs.append('a."ReferenceNo"')
            else:
                exprs.append(self._type_cast_expr(f'a."{cf["name"]}"', cf["type"], cf["label"]))
        for col in columns:
            label_info = labels.get(col, {})
            label = label_info.get("label", col)
            forms_type = label_info.get("forms_type", "") or ""
            if not forms_type or forms_type == "text":
                forms_type = self._infer_column_type(col, samples.get(col, []))
            exprs.append(self._type_cast_expr(f'combinedOri."{col}"', forms_type, label))
        return exprs

    @staticmethod
    def _assemble_form_sql(combined_cte: str, final_select: str,
                            resolved_core_fields: list[dict]) -> str:
        if not resolved_core_fields:
            return (
                f'WITH combinedOri AS ({combined_cte})\n'
                f'  SELECT\n  {final_select}\n'
                f'  FROM\n  combinedOri'
            )
        a_select_cols = ',\n      '.join(
            f'"public"."Applications"."{cf["name"]}"' for cf in resolved_core_fields
        )
        return (
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

    def _build_form_view_sql(self, base_name: str, db_id: int,
                             tenant_id: str,
                             core_fields: Optional[list[str]] = None,
                             selected_versions: Optional[list[str]] = None) -> tuple[str, list[str], dict, bool]:
        """
        Build deterministic SQL for Pattern 1 (Form Views).
        Returns (sql, column_labels, samples, needs_enhancement).
        """
        empty: tuple[str, list[str], dict, bool] = ("", [], {}, False)
        metadata = self.schema.get_metadata(db_id, tenant_id)
        all_groups = self._group_form_versions(metadata)

        versions, base_name = self._resolve_form_versions(base_name, metadata, all_groups)
        if versions is None:
            return empty

        if selected_versions:
            versions = [v for v in versions if v in selected_versions]
            if not versions:
                return empty

        primary_table = versions[0]
        primary_fields = self._get_reporting_table_fields(primary_table, metadata)
        columns = self._select_form_columns(primary_fields)
        if not columns:
            return empty

        labels = self.schema.get_view_labels(primary_table, db_id, tenant_id)
        wrapped_cols = self.schema.columns_needing_unwrap(
            "Reporting", primary_table, columns, db_id, tenant_id
        )
        combined_cte = self._build_union_cte(versions, columns, wrapped_cols)
        samples = self.schema.get_column_samples("Reporting", primary_table, columns, db_id, tenant_id)
        resolved_core_fields = self._resolve_core_fields(core_fields)

        final_select = ",\n  ".join(
            self._build_form_select_exprs(resolved_core_fields, columns, labels, samples)
        )
        sql = self._assemble_form_sql(combined_cte, final_select, resolved_core_fields)

        column_labels = [
            "ReferenceNo" if cf["name"] == "ReferenceNo" else cf["label"]
            for cf in resolved_core_fields
        ] + [labels.get(c, {}).get("label", c) for c in columns]

        # Any column without a real label falls back to its raw name → worth an AI
        # alias pass. When every column is labeled the deterministic SQL is already clean.
        needs_enhancement = any(not labels.get(c, {}).get("label") for c in columns)
        return sql, column_labels, samples, needs_enhancement

    def _find_worksheet_fk_column(self, view_name: str, target_fields: list[dict],
                                    db_id: int, tenant_id: str) -> Optional[str]:
        """Detect the Applications FK column on a worksheet/scoresheet view."""
        field_names = {f.get("name", "") for f in target_fields}
        for candidate in ("ApplicationId", "application_id", "CorrelationId", "correlation_id"):
            if candidate in field_names:
                return candidate
        return self.schema.find_application_id_column("Reporting", view_name, db_id, tenant_id)

    def _filter_worksheet_columns(self, target_fields: list[dict],
                                    fk_column: Optional[str]) -> list[str]:
        skip_cols = self.JUNK_COLUMNS | ({fk_column} if fk_column else set())
        columns = []
        for field in target_fields:
            col_name = field.get("name", "")
            if not col_name or col_name in skip_cols:
                continue
            if col_name.lower().endswith("_id"):
                continue
            if any(p in col_name.lower() for p in self.JUNK_PATTERNS):
                continue
            columns.append(col_name)
        return columns

    def _build_worksheet_select_exprs(self, resolved_core_fields: list[dict],
                                        columns: list[str], labels: dict,
                                        custom_labels: dict, samples: dict) -> list[str]:
        exprs = []
        for cf in resolved_core_fields:
            if cf["name"] == "ReferenceNo":
                exprs.append('a."ReferenceNo"')
            else:
                exprs.append(self._type_cast_expr(f'a."{cf["name"]}"', cf["type"], cf["label"]))
        for col in columns:
            label_info = labels.get(col, {})
            label = label_info.get("label", "") or custom_labels.get(col, "") or col
            forms_type = label_info.get("forms_type", "") or ""
            if not forms_type or forms_type == "text":
                forms_type = self._infer_column_type(col, samples.get(col, []))
            exprs.append(self._type_cast_expr(f'"t"."{col}"', forms_type, label))
        return exprs

    @staticmethod
    def _assemble_worksheet_sql(select_clause: str, view_name: str,
                                  join_condition: Optional[str], has_core_fields: bool) -> str:
        base = (
            f'SELECT\n  {select_clause}\n'
            f'FROM\n  "Reporting"."{view_name}" AS "t"'
        )
        if join_condition and has_core_fields:
            return f'{base}\nLEFT JOIN "public"."Applications" AS a ON {join_condition}'
        return base

    def _worksheet_column_labels(self, resolved_core_fields: list[dict], columns: list[str],
                                   labels: dict, custom_labels: dict) -> tuple[list[str], bool]:
        column_labels = [
            "ReferenceNo" if cf["name"] == "ReferenceNo" else cf["label"]
            for cf in resolved_core_fields
        ]
        needs_enhancement = False
        for col in columns:
            label_info = labels.get(col, {})
            real_label = label_info.get("label", "") or custom_labels.get(col, "")
            column_labels.append(real_label or col)
            if not real_label:
                needs_enhancement = True
        return column_labels, needs_enhancement

    def _build_worksheet_view_sql(self, view_name: str, db_id: int,
                                   tenant_id: str,
                                   core_fields: Optional[list[str]] = None) -> tuple[str, list[str], dict, bool]:
        """
        Build deterministic SQL for worksheet/scoresheet views.
        Simpler than form views: no UNION ALL, no JSON unwrap.
        Just SELECT columns with optional JOIN to Applications for core fields.
        Returns (sql, column_labels, samples, needs_enhancement).
        """
        empty: tuple[str, list[str], dict, bool] = ("", [], {}, False)
        metadata = self.schema.get_metadata(db_id, tenant_id)

        target_fields = self._get_reporting_table_fields(view_name, metadata)
        if not target_fields:
            return empty

        labels = self.schema.get_view_labels(view_name, db_id, tenant_id)
        custom_labels = {} if labels else self.schema.get_custom_field_labels(db_id, tenant_id)

        fk_column = self._find_worksheet_fk_column(view_name, target_fields, db_id, tenant_id)
        join_condition = f'"t"."{fk_column}" = a."Id"' if fk_column else None

        columns = self._filter_worksheet_columns(target_fields, fk_column)
        if not columns:
            return empty

        samples = self.schema.get_column_samples("Reporting", view_name, columns, db_id, tenant_id)
        resolved_core_fields = self._resolve_core_fields(core_fields) if join_condition else []

        select_clause = ",\n  ".join(
            self._build_worksheet_select_exprs(
                resolved_core_fields, columns, labels, custom_labels, samples
            )
        )
        sql = self._assemble_worksheet_sql(
            select_clause, view_name, join_condition, bool(resolved_core_fields)
        )
        column_labels, needs_enhancement = self._worksheet_column_labels(
            resolved_core_fields, columns, labels, custom_labels
        )
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
                text = _strip_trailing_code_fence(text)
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
        metadata = self.schema.get_metadata(db_id, tenant_id)
        tables_by_name = {
            t.get("name"): t for t in metadata.get("tables", [])
            if t.get("schema") == "Reporting"
        }

        views_with_labels, correlation_providers = self.schema.get_report_columns_maps_info(db_id, tenant_id)

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
        has_data_map = self.schema.batch_check_view_data(physical_names, db_id, tenant_id)

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

    @staticmethod
    def _empty_preview(view_name: str, description: str, error: str) -> dict:
        return {
            "name": view_name,
            "description": description,
            "sql": "",
            "valid": False,
            "error": error,
            "source_view": view_name,
            "columns": [],
            "excluded_columns": [],
        }

    def _build_deterministic_sql(self, source_type: str, view_name: str, db_id: int,
                                   tenant_id: str, core_fields: Optional[list[str]],
                                   selected_versions: Optional[list[str]]
                                   ) -> Optional[tuple[str, list[str], dict, bool]]:
        """Route to the right deterministic builder. Returns None when the
        caller should fall through to the full AI pipeline."""
        if source_type == "form_view":
            match = FORM_VERSION_RE.match(view_name)
            base_name = match.group(1) if match else view_name
            return self._build_form_view_sql(
                base_name, db_id, tenant_id, core_fields, selected_versions
            )
        if source_type in ("worksheet_view", "scoresheet_view", "other_view"):
            return self._build_worksheet_view_sql(view_name, db_id, tenant_id, core_fields)
        return None

    async def _enhance_or_name(self, sql: str, samples: dict, view_name: str,
                                 columns: list[str], needs_enhancement: bool
                                 ) -> tuple[str, Optional[str], Optional[str]]:
        """Run the single LLM call that produces a name/description (and
        optionally a cleaned-up SQL). Returns (sql, name, description)."""
        if needs_enhancement:
            try:
                enhanced_sql, name, description = await self._enhance_and_name(sql, samples)
            except Exception as e:
                logger.warning("AI enhance+name failed — keeping deterministic SQL: %s", e)
                return sql, None, None
            if enhanced_sql and self._validate_enhancement(sql, enhanced_sql):
                sql = enhanced_sql
            return sql, name, description

        try:
            name, description = await self._generate_name_description(view_name, columns)
            return sql, name, description
        except Exception as e:
            logger.warning("AI naming failed — using fallback name: %s", e)
            return sql, None, None

    async def preview_model(self, view_name: str, db_id: int,
                            tenant_id: str,
                            core_fields: Optional[list[str]] = None,
                            selected_versions: Optional[list[str]] = None) -> dict:
        """
        Step 1: Generate a model proposal for a selected view.

        Routes to deterministic builders for known patterns, falls back to AI.
        """
        source_type = self._detect_source_type(view_name, db_id, tenant_id)
        build_result = self._build_deterministic_sql(
            source_type, view_name, db_id, tenant_id, core_fields, selected_versions
        )
        if build_result is None:
            return await self._preview_model_ai(view_name, db_id, tenant_id)

        sql, columns, samples, needs_enhancement = build_result
        if not sql:
            return self._empty_preview(
                view_name, "Could not build model SQL", "No columns or source data found"
            )

        # One LLM call per preview — enhance+name when columns lack labels,
        # otherwise a cheaper naming-only call.
        sql, name, description = await self._enhance_or_name(
            sql, samples, view_name, columns, needs_enhancement
        )

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

    @classmethod
    def _combined_error(cls, view_names: list[str], error: str) -> dict:
        return {
            "name": cls.COMBINED_MODEL_NAME,
            "description": cls.COMBINED_MODEL_BUILD_FAILED_DESC,
            "sql": "",
            "valid": False,
            "error": error,
            "source_view": " + ".join(view_names),
            "columns": [],
            "excluded_columns": [],
        }

    def _effective_combined_core_fields(self, core_fields: Optional[list[str]]) -> list[str]:
        """Ensure ReferenceNo (the JOIN key) is present in the core-field list."""
        if core_fields is None:
            return self._default_core_field_names()
        effective = list(core_fields)
        if "ReferenceNo" not in effective:
            effective.insert(0, "ReferenceNo")
        return effective

    def _build_view_sql_for_combine(self, vn: str, db_id: int, tenant_id: str,
                                      effective_core_fields: list[str]) -> tuple[str, list[str]]:
        """Build the per-view SQL + column list used as a CTE in a combined model."""
        source_type = self._detect_source_type(vn, db_id, tenant_id)
        if source_type == "form_view":
            match = FORM_VERSION_RE.match(vn)
            base_name = match.group(1) if match else vn
            sql, cols, _, _ = self._build_form_view_sql(
                base_name, db_id, tenant_id, effective_core_fields
            )
        else:
            sql, cols, _, _ = self._build_worksheet_view_sql(
                vn, db_id, tenant_id, effective_core_fields
            )
        return sql, cols

    def _build_combined_views(self, view_names: list[str], db_id: int, tenant_id: str,
                                effective_core_fields: list[str]
                                ) -> list[tuple[str, str, list[str]]]:
        built: list[tuple[str, str, list[str]]] = []
        for vn in view_names:
            sql, cols = self._build_view_sql_for_combine(
                vn, db_id, tenant_id, effective_core_fields
            )
            if not sql:
                logger.warning("Could not build SQL for '%s' in combined model — skipping", vn)
                continue
            built.append((vn, sql, cols))
        return built

    @staticmethod
    def _partition_joinable(built: list[tuple[str, str, list[str]]]
                             ) -> tuple[list[tuple[str, str, list[str]]], list[str]]:
        """Return (joinable_with_primary, skipped_view_names). Joinable views all
        have a ReferenceNo column; the primary is always included."""
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
        return joinable, skipped

    @staticmethod
    def _build_combined_select(joinable: list[tuple[str, str, list[str]]]
                                 ) -> tuple[list[str], list[str]]:
        """Build (select_exprs, dropped_alias_notes) — dedupes column aliases
        across views and reports which dups were dropped."""
        _, _, primary_cols = joinable[0]
        used_lower = {c.lower() for c in primary_cols}
        select_exprs = [f'view_1."{col}"' for col in primary_cols]
        dropped_cols: list[str] = []

        for i, (vn, _, cols) in enumerate(joinable[1:], start=2):
            alias = f"view_{i}"
            for col in cols:
                low = col.lower()
                if low not in used_lower:
                    select_exprs.append(f'{alias}."{col}"')
                    used_lower.add(low)
                elif low != "referenceno":
                    dropped_cols.append(f"{col} (from {vn})")
        return select_exprs, dropped_cols

    @staticmethod
    def _assemble_combined_sql(joinable: list[tuple[str, str, list[str]]],
                                 select_exprs: list[str]) -> str:
        cte_parts = [
            f"view_{i + 1} AS (\n{sql}\n)"
            for i, (_, sql, _) in enumerate(joinable)
        ]
        join_clauses = [
            f'LEFT JOIN view_{i} ON view_{i}."ReferenceNo" = view_1."ReferenceNo"'
            for i in range(2, len(joinable) + 1)
        ]
        return (
            f"WITH\n{',\n'.join(cte_parts)}\n"
            f"SELECT\n  {',\n  '.join(select_exprs)}\n"
            f"FROM view_1\n"
            + "\n".join(join_clauses)
        )

    def _annotate_combined_description(self, description: Optional[str], skipped: list[str],
                                          joinable: list[tuple[str, str, list[str]]],
                                          db_id: int, tenant_id: str) -> str:
        annotated = description or ""
        if skipped:
            annotated += f" (skipped — no \"ReferenceNo\" join key: {', '.join(skipped)})"
        fan_out_views = [
            vn for vn, _, _ in joinable[1:]
            if self._detect_source_type(vn, db_id, tenant_id)
            in ("worksheet_view", "scoresheet_view")
        ]
        if fan_out_views:
            annotated += (
                f" ⚠ Combining with multi-row-per-application view(s) "
                f"{', '.join(fan_out_views)} may duplicate the primary view's rows."
            )
        return annotated

    async def preview_combined_model(self, view_names: list[str], db_id: int,
                                     tenant_id: str,
                                     core_fields: Optional[list[str]] = None) -> dict:
        """
        Generate a single merged model SQL from 2+ views joined via "ReferenceNo".

        Each view's SQL is wrapped as a named CTE (PostgreSQL supports nested CTEs
        inside CTE bodies). Secondary views are LEFT JOINed to the primary view
        on "ReferenceNo". Column aliases are deduplicated.
        """
        effective_core_fields = self._effective_combined_core_fields(core_fields)

        if len(view_names) < 2:
            return await self.preview_model(view_names[0], db_id, tenant_id, effective_core_fields)

        built = self._build_combined_views(view_names, db_id, tenant_id, effective_core_fields)
        if len(built) < 2:
            if built:
                return await self.preview_model(built[0][0], db_id, tenant_id, effective_core_fields)
            return self._combined_error(view_names, "No valid SQL could be built for the selected views")

        primary_vn, _, primary_cols = built[0]
        if "referenceno" not in {c.lower() for c in primary_cols}:
            return self._combined_error(
                view_names,
                f"Primary view '{primary_vn}' has no \"ReferenceNo\" column to join on. "
                f"Combine requires views that link to public.Applications."
            )

        joinable, skipped = self._partition_joinable(built)
        if len(joinable) < 2:
            return self._combined_error(
                view_names,
                f"None of the secondary views share a \"ReferenceNo\" join key with "
                f"'{primary_vn}'. Skipped: {', '.join(skipped)}."
            )

        select_exprs, dropped_cols = self._build_combined_select(joinable)
        combined_sql = self._assemble_combined_sql(joinable, select_exprs)

        combined_sql, is_valid, error, preview_raw = await self._validate_sql_with_heal(
            combined_sql, db_id, tenant_id, heal_args=None
        )

        all_cols = list({c for _, _, cols in joinable for c in cols})
        source_label = " + ".join(vn for vn, _, _ in joinable)
        name, description = await self._generate_name_description(source_label, all_cols)
        description = self._annotate_combined_description(
            description, skipped, joinable, db_id, tenant_id
        )

        logger.info(
            "Combined model preview complete - tenant=%s views=%s valid=%s",
            tenant_id, view_names, is_valid
        )

        proposal = {
            "name": name,
            "description": description,
            "sql": combined_sql,
            "valid": is_valid,
            "error": error,
            "source_view": " + ".join(vn for vn, _, _ in joinable),
            "columns": all_cols,
            "excluded_columns": dropped_cols,
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

    def _builder_core_fields_for_modification(
        self, additional_view_names: list[str], core_fields: Optional[list[str]]
    ) -> Optional[list[str]]:
        """Additional view CTEs must include ReferenceNo as JOIN key. The AI
        prompt still sees the literal user selection — this is internal plumbing."""
        if not additional_view_names:
            return core_fields
        if not core_fields:
            return self._default_core_field_names()
        builder = list(core_fields)
        if "ReferenceNo" not in builder:
            builder.insert(0, "ReferenceNo")
        return builder

    def _build_additional_views_text(self, additional_view_names: list[str], db_id: int,
                                       tenant_id: str,
                                       builder_core_fields: Optional[list[str]]) -> str:
        if not additional_view_names:
            return "(none)"
        view_sqls: list[str] = []
        for vn in additional_view_names:
            source_type = self._detect_source_type(vn, db_id, tenant_id)
            if source_type == "form_view":
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
        return "\n\n".join(view_sqls) if view_sqls else "(none)"

    def _format_core_fields_for_prompt(self, core_fields: Optional[list[str]]) -> str:
        resolved = self._resolve_core_fields(core_fields) if core_fields else []
        if not resolved:
            return "(none)"
        return "\n".join(
            f'  "{cf["name"]}" → alias '
            f'"{cf["name"] if cf["name"] == "ReferenceNo" else cf["label"]}" '
            f'(type: {cf["type"]})'
            for cf in resolved
        )

    @staticmethod
    def _pick_unique_variant_name(current_name: str, existing_names: set[str]) -> str:
        candidate = f"{current_name} v2"
        if candidate not in existing_names:
            return candidate
        for n in range(3, 100):
            candidate = f"{current_name} v{n}"
            if candidate not in existing_names:
                return candidate
        return f"{current_name} v2"

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
        if not prompt and not additional_view_names and core_fields is None:
            raise ValueError(
                "At least one of prompt, additional_view_names, or core_fields is required"
            )

        card = metabase_client.get_card(card_id, tenant_id)
        current_name = card.get("name", "Unnamed Model")
        query_type = card.get("dataset_query", {}).get("type", "unknown")

        current_sql, current_columns = metabase_client.card_sql_and_columns(
            card, db_id, tenant_id
        )
        if not current_sql:
            raise ValueError(
                f"Could not extract SQL from existing model (query type='{query_type}'). "
                "Metabase may not support converting this structured query to native SQL."
            )

        builder_core_fields = self._builder_core_fields_for_modification(
            additional_view_names, core_fields
        )
        additional_views_text = self._build_additional_views_text(
            additional_view_names, db_id, tenant_id, builder_core_fields
        )
        core_fields_text = self._format_core_fields_for_prompt(core_fields)

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
            raise RuntimeError(
                "AI service failed to generate modified SQL — "
                "the LLM request timed out or returned an error"
            )

        new_sql = re.sub(r"^```(?:sql)?\s*", "", new_sql.strip(), flags=re.IGNORECASE)
        new_sql = _strip_trailing_code_fence(new_sql)

        columns_text = "\n".join(f"  {c}" for c in current_columns)
        new_sql, is_valid, error, preview_raw = await self._validate_sql_with_heal(
            new_sql, db_id, tenant_id,
            heal_args=(
                columns_text,
                "(modification — preserve existing JOIN structure)",
                "(modification — preserve existing relationships)",
            ),
        )

        existing_names = metabase_client.get_all_card_names(tenant_id)
        new_name = self._pick_unique_variant_name(current_name, set(existing_names))
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
            except Exception:
                # Full detail is logged server-side; the client only gets a
                # generic message so exception text isn't exposed externally.
                logger.exception(f"Failed to create model '{name}'")
                errors.append({
                    "name": name,
                    "error": "Failed to create model due to an internal error",
                })

        logger.info(
            "Model creation complete - tenant=%s models_created=%d models_errored=%d",
            tenant_id, len(created), len(errors)
        )
        return {"created": created, "errors": errors}

    # --- Private helpers ---

    @staticmethod
    def _build_field_id_lookup(metadata: dict) -> dict[int, tuple[str, str, str]]:
        """field_id → (schema, table, column) across every table in metadata."""
        lookup: dict[int, tuple[str, str, str]] = {}
        for table in metadata.get("tables", []):
            t_schema = table.get("schema", "public")
            t_name = table.get("name", "")
            for field in table.get("fields", []):
                fid = field.get("id")
                if fid:
                    lookup[fid] = (t_schema, t_name, field.get("name", ""))
        return lookup

    @staticmethod
    def _find_reporting_table(view_name: str, metadata: dict) -> Optional[dict]:
        for table in metadata.get("tables", []):
            if table.get("name") == view_name and table.get("schema") == "Reporting":
                return table
        return None

    _KNOWN_FK_FALLBACK = {
        "ApplicationId": ("public", "Applications", "Id"),
        "CorrelationId": ("public", "Applications", "Id"),
        "ApplicantId": ("public", "Applicants", "Id"),
    }

    def _resolve_fk_map(self, fields: list[dict], field_lookup: dict,
                          schema_name: str, view_name: str,
                          db_id: int, tenant_id: str
                          ) -> tuple[dict[str, tuple[str, str, str]], set[str]]:
        """Returns (fk_map, field_names). field_names may grow if a probe finds
        an FK column not declared in metadata."""
        field_names = {f.get("name", "") for f in fields}
        fk_map: dict[str, tuple[str, str, str]] = {}
        for field in fields:
            col_name = field.get("name", "")
            fk_target_id = field.get("fk_target_field_id")
            if fk_target_id and fk_target_id in field_lookup:
                fk_map[col_name] = field_lookup[fk_target_id]

        if not fk_map:
            for col_name, target in self._KNOWN_FK_FALLBACK.items():
                if col_name in field_names:
                    fk_map[col_name] = target

        if not fk_map:
            app_id_col = self.schema.find_application_id_column(
                schema_name, view_name, db_id, tenant_id
            )
            if app_id_col:
                fk_map[app_id_col] = ("public", "Applications", "Id")
                field_names.add(app_id_col)

        return fk_map, field_names

    def _format_displayable_columns(self, fields: list[dict], join_key_names: set[str],
                                      view_labels: dict, custom_labels: dict,
                                      schema_name: str, view_name: str,
                                      db_id: int, tenant_id: str) -> str:
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
            sample = self.schema.get_sample_value(
                schema_name, view_name, col_name, db_id, tenant_id
            )
            sample_str = f'"{sample[:60]}"' if sample else "(no data yet)"
            lines.append(f"  {col_name} | {db_type} | {label or '(no label)'} | {sample_str}")
        return "\n".join(lines)

    @staticmethod
    def _format_join_keys_and_relationships(
        fk_map: dict[str, tuple[str, str, str]], view_name: str
    ) -> tuple[str, str]:
        if not fk_map:
            return (
                "(none — this view has no FK columns; query directly without joins)",
                "(none — query the view directly without joins)",
            )

        join_keys_text = "\n".join(
            f'  "{col}" (FK → "{ts}"."{tt}"."{tc}" — JOIN only, do not SELECT)'
            for col, (ts, tt, tc) in sorted(fk_map.items())
        )
        rel_lines = [
            f'- "{view_name}"."{col}" = "{ts}"."{tt}"."{tc}"'
            for col, (ts, tt, tc) in sorted(fk_map.items())
        ]
        fk_targets = {(s, t) for s, t, _ in fk_map.values()}
        if ("public", "Applications") in fk_targets:
            rel_lines.append('- "public"."Applications"."ApplicantId" → "public"."Applicants"."Id"')
            rel_lines.append('- "public"."ApplicantAgents"."ApplicantId" = "public"."Applicants"."Id" AND "public"."ApplicantAgents"."ApplicationId" = "public"."Applications"."Id"')
            rel_lines.append('- "public"."ApplicantAddresses"."ApplicantId" = "public"."Applicants"."Id" AND "public"."ApplicantAddresses"."ApplicationId" = "public"."Applications"."Id"')
        return join_keys_text, "\n".join(rel_lines)

    def _build_prompt_context(self, view_name: str, db_id: int,
                              tenant_id: str) -> tuple[str, str, str, str]:
        """
        Assemble column info for the AI prompt using live Metabase metadata.

        Resolves FK relationships from fk_target_field_id — no hardcoded column names.

        Returns (columns_text, schema_name, join_keys_text, relationships_text).
        """
        metadata = self.schema.get_metadata(db_id, tenant_id)
        field_lookup = self._build_field_id_lookup(metadata)

        target_table = self._find_reporting_table(view_name, metadata)
        if not target_table:
            logger.error(f"View '{view_name}' not found in metadata")
            return "", "Reporting", "(none)", "(none — query the view directly without joins)"

        schema_name = target_table.get("schema", "Reporting")
        fields = target_table.get("fields", [])

        view_labels = self.schema.get_view_labels(view_name, db_id, tenant_id)
        custom_labels = {} if view_labels else self.schema.get_custom_field_labels(db_id, tenant_id)

        fk_map, field_names = self._resolve_fk_map(
            fields, field_lookup, schema_name, view_name, db_id, tenant_id
        )
        logger.info(f"FK map for {view_name}: {fk_map}")

        join_key_names = set(fk_map.keys()) | (self.JUNK_COLUMNS & field_names)
        columns_text = self._format_displayable_columns(
            fields, join_key_names, view_labels, custom_labels,
            schema_name, view_name, db_id, tenant_id,
        )
        join_keys_text, relationships_text = self._format_join_keys_and_relationships(
            fk_map, view_name
        )
        return columns_text, schema_name, join_keys_text, relationships_text

    def _extract_column_info(self, view_name: str, db_id: int,
                             tenant_id: str, sql: str) -> tuple[list[str], list[str]]:
        """
        Compare view columns against generated SQL to determine included/excluded.

        Returns (included_columns, excluded_columns).
        """
        metadata = self.schema.get_metadata(db_id, tenant_id)

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
        result = _strip_trailing_code_fence(result)
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
            logger.exception(f"LLM request failed: {e}")
            return None

    def _parse_single_definition(self, raw: str) -> dict:
        """Parse AI response into a single {name, description, sql} dict."""
        text = re.sub(r"^```(?:json)?\s*", "", raw.strip(), flags=re.IGNORECASE)
        text = _strip_trailing_code_fence(text)

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
