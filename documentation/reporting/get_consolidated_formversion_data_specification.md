# get_consolidated_formversion_data.sql Function Specification

## Overview

`get_consolidated_formversion_data` is a PostgreSQL PL/pgSQL function that generates a dynamic SQL query spanning **all versions** of a CHEFS form. It is called by `generate_consolidated_formversion_view`. Rather than targeting a single form version, it iterates over every version recorded in the mapping metadata and generates a query per version, combining them with `UNION ALL`. Each output row includes a `form_version_label` column identifying which version it came from.

## Function Signature

```sql
CREATE OR REPLACE FUNCTION "Reporting".get_consolidated_formversion_data(
    form_id uuid,
    report_map_id uuid
)
RETURNS text
LANGUAGE plpgsql
```

## Purpose

- Generates a multi-version UNION ALL query covering all form versions for a given form
- Reads version IDs and labels from the mapping's `Metadata.Info` dictionary
- Applies **version gating** — fields that belong to specific versions produce NULLs for other versions
- Inherits the dual-schema (legacy/current) UNION ALL pattern from `get_formversion_data`
- Supports both root fields and DataGrid fields
- Returns a complete SQL query string ready for use as a consolidated view body

## Input Parameters

- **form_id** — UUID of the application form (not a form version ID). Determines which versions to include via the metadata keys.
- **report_map_id** — UUID of the `ReportColumnsMaps` record containing the consolidated mapping configuration.

## Return Value

Returns a `TEXT` string containing a UNION ALL query across all form versions and field types.

---

## Version Iteration

Form version IDs and labels are stored in the mapping's `Metadata.Info` dictionary at save time, keyed as `formversion_{versionId}` with the version label as the value (e.g., `v1`, `v2`).

The function reads these at query time:

```sql
SELECT replace(kv.key, 'formversion_', '')::uuid, kv.value
FROM "Reporting"."ReportColumnsMaps" rcm,
     jsonb_each_text(rcm."Mapping"->'Metadata'->'Info') kv
WHERE rcm."Id" = report_map_id
  AND kv.key LIKE 'formversion_%'
ORDER BY kv.value
```

For each version, one or more query pairs (root + DataGrid, each legacy + current) are generated and accumulated. All are combined into a final `UNION ALL`.

---

## Version Gating

Fields in a consolidated mapping carry a `VersionLabel` that indicates which versions they belong to:

| VersionLabel | Meaning |
| ------------ | ------- |
| `NULL` | Field is present and identical in **all** versions — always emit the real value |
| `'v1'` | Field exists only in version 1 — emit real value for v1, NULL for other versions |
| `'v1, v3'` | Field exists in v1 and v3 — emit real value for those, NULL for others |

When processing a specific version, any field whose `VersionLabel` is non-null and does not contain the current `version_lbl` is **skipped** — its NULL placeholder is left in place rather than replaced with a data extraction expression.

---

## Dual-Schema Design

Identical to `get_formversion_data`: each version generates two query branches (legacy and current submission schema), split by whether `submission->'submission'->'submission'` IS NULL:

- **Legacy**: `afs."Submission"->'submission'->'data'`
- **Current**: `afs."Submission"->'submission'->'submission'->'data'`

Each pair is combined with `UNION ALL` before the per-version UNION ALL accumulation.

---

## Core Processing Logic

### Pass 1 — Column inventory and DataGrid detection

Same as `get_formversion_data`: collect all unique column names, types, and DataGrid names.

No type-conflict fallback — the consolidated function uses the first occurrence of each column name's type and does not emit TEXT fallback for conflicts (fields from different versions with different types are handled via version gating instead).

### Pass 2 — NULL column list construction

Builds a typed NULL placeholder list ordered by column name, same as other functions.

### Pass 3 — Per-version query generation

For each version (ordered by label):

1. Initialise SELECT with fixed columns + typed NULL placeholders
2. Add `form_version_label` as a literal string constant
3. For each field in the mapping, check version gating — skip if this version is not in the field's `VersionLabel`
4. Replace NULL placeholders with JSON extraction expressions for applicable fields
5. Emit a legacy + current UNION ALL pair for root fields
6. Emit a legacy + current UNION ALL pair per DataGrid
7. Append all version queries to the accumulation array

### Final assembly

All accumulated queries are combined with `UNION ALL`.

---

## Data Sources

- `public."ApplicationFormSubmissions"` — filtered by `ApplicationFormVersionId = version_id` for each version in the loop

---

## Output Columns

| Column | Source | Description |
| ------ | ------ | ----------- |
| `submission_id` | `afs."Id"` | UUID of the form submission record |
| `application_id` | `afs."ApplicationId"` | UUID of the parent application |
| `row_identifier` | `'root'` or `'{datagrid}_r{n}'` | Row type identifier |
| `form_version_label` | literal from metadata | Version label (e.g., `'v1'`, `'v2'`) |
| *(dynamic columns)* | Extracted from JSON | One column per mapped field; NULL for versions where the field doesn't exist |

---

## Field Type Handling

Same as `get_formversion_data`:

| Type | SQL Type |
| ---- | -------- |
| `textfield` / `textarea` / `email` / `select` / `phoneNumber` | TEXT |
| `number` | NUMERIC |
| `currency` | DECIMAL(18,2) |
| `option` / `checkbox` | BOOLEAN |

---

## Differences from get_formversion_data

| Aspect | `get_consolidated_formversion_data` | `get_formversion_data` |
| ------ | ----------------------------------- | ---------------------- |
| Parameter | `form_id` (all versions) | `correlation_id` (one version) |
| Version loop | Iterates over metadata keys | No loop — single version |
| Version gating | Yes — per-field NULL for other versions | Not applicable |
| Extra output column | `form_version_label` | Not present |
| Type conflict handling | First-occurrence wins (no TEXT fallback) | TEXT fallback for conflicts |

---

## Common Use Cases

### 1. Field present in all versions (VersionLabel = null)

The field produces a real value for every version's query — no NULLs due to version gating.

### 2. Field exclusive to one version

Produces a real value only in that version's query; all other version queries leave it as a typed NULL.

### 3. Field with type conflict across versions

The first occurrence's type is used for the NULL column list. The field may be a NULL for versions where it doesn't apply, avoiding the conflict at query time.

---

## Debugging Queries

```sql
-- Check what versions are stored in the mapping metadata
SELECT kv.key, kv.value
FROM "Reporting"."ReportColumnsMaps" rcm,
     jsonb_each_text(rcm."Mapping"->'Metadata'->'Info') kv
WHERE rcm."Id" = '<report_map_id>'
  AND kv.key LIKE 'formversion_%';

-- Check field VersionLabels in the mapping
SELECT row_data->>'ColumnName', row_data->>'VersionLabel'
FROM "Reporting"."ReportColumnsMaps" rcm,
     jsonb_array_elements(rcm."Mapping"->'Rows') row_data
WHERE rcm."Id" = '<report_map_id>';

-- Test function output
SELECT "Reporting".get_consolidated_formversion_data('<form_id>', '<report_map_id>');
```
