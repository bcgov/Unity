"""
Prompt templates for the AI-assisted data model generator.

Extracted from model_generator.py so the prompt text lives in one place and the
generator module stays focused on workflow logic. These are plain `str.format`
templates — keep the `{placeholder}` names in sync with their call sites.
"""

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

MODIFY_PROMPT = """You are modifying an existing data model SQL query. Apply the user's
change request below and return the full rewritten query.

CURRENT SQL:
{current_sql}

CURRENT OUTPUT COLUMNS (the columns this query currently returns):
{current_columns}

USER'S CHANGE REQUEST (this is the primary instruction — apply it in full):
{user_prompt}

CORE FIELDS TO INCLUDE (from "public"."Applications" — ensure these are present
in the final SELECT, adding a LEFT JOIN if not already joined):
{core_fields_text}

ADDITIONAL VIEW SQL TO INTEGRATE (wrap each as a named CTE and LEFT JOIN to the
existing SQL via "ReferenceNo"; if a view has no "ReferenceNo" column, omit it):
{additional_views_text}

RULES:
- Apply the USER'S CHANGE REQUEST in full, then leave every other existing column
  unchanged. Only add, remove, or alter the columns the request actually calls for.
- REMOVING a column: the query's output is determined ONLY by the final (outermost)
  SELECT list. To drop a column the user named, delete its entry from that final
  SELECT and nothing else — you do NOT need to touch inner CTE definitions, and the
  column staying defined inside a CTE is fine as long as the final SELECT no longer
  references it. Match the user's wording to the closest entry in CURRENT OUTPUT
  COLUMNS. In combined models the final SELECT lists each output column as
  `view_N."Column Name"` (with no AS alias) — delete that whole `view_N."..."` entry.
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
