# get_consolidated_worksheet_data.sql Function Specification

## Overview

`get_consolidated_worksheet_data` is a PostgreSQL PL/pgSQL function (implemented as pure CTE-based SQL) that generates a dynamic SQL query spanning **all form versions** for a consolidated worksheet view. It is called by `generate_consolidated_worksheet_view`. Like `get_worksheet_data`, it reads from `Flex.WorksheetInstances`, but iterates over every form version recorded in the mapping metadata, applying version gating per field, and adds a `form_version_label` column to each output row.

## Function Signature

```sql
CREATE OR REPLACE FUNCTION "Reporting".get_consolidated_worksheet_data(
    form_id uuid,
    report_map_id uuid
)
RETURNS text
LANGUAGE plpgsql
```

## Purpose

- Generates a multi-version UNION ALL query covering worksheet data from all form versions for a given form
- Reads version IDs and labels from the mapping's `Metadata.Info` dictionary
- Reads worksheet link IDs from the same metadata (`ws_{versionId}_{worksheetId}` keys)
- Applies **version gating** — fields for specific versions produce NULLs in queries for other versions
- Supports both root fields and DataGrid fields per worksheet per version
- Implemented as a CTE-based approach (like `get_worksheet_data`), not imperative PL/pgSQL loops
- Returns a complete SQL query string ready for use as a view body

## Input Parameters

- **form_id** — UUID of the application form. Determines which versions and worksheets to include via the metadata.
- **report_map_id** — UUID of the `ReportColumnsMaps` record containing the consolidated mapping configuration.

## Return Value

Returns a `TEXT` string containing a UNION ALL query across all (version, worksheet, field-group) combinations.

---

## Version and Worksheet Iteration

### Version info

Version IDs and labels are stored in `Metadata.Info` keyed as `formversion_{versionId}` → `v1`, `v2`, etc.:

```sql
-- version_info CTE
SELECT replace(kv.key, 'formversion_', '')::uuid AS version_id, kv.value AS version_label
FROM "Reporting"."ReportColumnsMaps" rcm,
     jsonb_each_text(rcm."Mapping"->'Metadata'->'Info') kv
WHERE rcm."Id" = report_map_id
  AND kv.key LIKE 'formversion_%'
```

### Worksheet links

Worksheet link IDs are stored keyed as `ws_{versionId}_{worksheetId}` → worksheet display info. These are used to scope worksheet instance queries to specific versions.

---

## Version Gating

Fields carry a `VersionLabel` indicating which versions they belong to (same semantics as the consolidated formversion function):

| VersionLabel | Meaning |
| ------------ | ------- |
| `NULL` | Field present and identical in **all** versions — always emit the real value |
| `'v1'` | Field exclusive to version 1 |
| `'v1, v3'` | Field present in v1 and v3 only |

When generating a query for a specific version, fields whose `VersionLabel` does not include the current version label are left as typed NULL placeholders.

---

## CTE-Based Implementation

Unlike `get_consolidated_formversion_data` (which uses PL/pgSQL loops), this function uses a nested CTE structure to build and assemble query fragments. The key CTEs are:

| CTE | Purpose |
| --- | ------- |
| `mapping_data` | Extracts all mapping rows from `ReportColumnsMaps`, parses worksheet name and clean DataPath |
| `unique_mappings` | `DISTINCT ON (column_name)` — one mapping per column |
| `version_info` | Reads form version IDs and labels from metadata |
| `unique_worksheet_names` | Distinct worksheet names from the mapping |
| `unique_datagrid_combinations` | Distinct `(worksheet_name, datagrid_name)` pairs for DataGrid fields |
| `datagrid_queries` | One query fragment per `(version, worksheet, datagrid)` triple |
| `root_queries` | One query fragment per `(version, worksheet)` for root-level fields |
| *(final assembly)* | `UNION ALL` of datagrid and root query fragments |

---

## Data Sources

| Table | Schema | Purpose |
| ----- | ------ | ------- |
| `WorksheetInstances` | `Flex` | Worksheet data per application; `CorrelationId` = application ID |
| `Worksheets` | `Flex` | Worksheet metadata (name, title) |
| `ReportColumnsMaps` | `Reporting` | Mapping configuration and metadata |

---

## Output Columns

| Column | Source | Description |
| ------ | ------ | ----------- |
| `worksheet_instance_id` | `wi."Id"` | UUID of the worksheet instance |
| `application_id` | `wi."CorrelationId"` | UUID of the parent application |
| `worksheet_name` | `w."Name"` | Worksheet name from definition |
| `row_identifier` | `'root'` or `'{datagrid}_r{n}'` | Row type identifier |
| `form_version_label` | literal from metadata | Version label (e.g., `'v1'`, `'v2'`) |
| *(dynamic columns)* | Extracted from `CurrentValue` | One column per mapped field; NULL for non-applicable versions |

---

## DataPath and Worksheet Name Parsing

The CTE extracts `worksheet_name` and `clean_data_path` from the stored `DataPath` and `Path` fields:

```sql
CASE
  WHEN data_path_raw ~ '^\('         -- starts with (worksheet_name)
  THEN substring(data_path_raw from '^\(([^)]+)\)')  -- extract from parentheses
  ELSE split_part(path, '->', 1)     -- first segment of Path
END AS worksheet_name,

CASE
  WHEN data_path_raw ~ '^\('
  THEN regexp_replace(data_path_raw, '^\([^)]+\)', '')  -- strip leading (name)
  ELSE data_path_raw
END AS clean_data_path
```

DataPath format for worksheet fields: `(WorksheetName)FieldName` or `(WorksheetName)DataGridName->FieldName`.

---

## Field Type Handling

Same as `get_worksheet_data`:

| Type | SQL Type | Extraction |
| ---- | -------- | ---------- |
| `text` / `textarea` | TEXT | Direct string from `CurrentValue` |
| `currency` | DECIMAL(18,2) | Locale commas stripped, numeric validated |
| `number` | NUMERIC | Numeric validated |
| `date` | TIMESTAMP | Date validated |
| `checkbox` | BOOLEAN | True/false value list |
| `checkboxgroup` | BOOLEAN per option | JSON array parsed, individual option extracted |
| `radio` | TEXT | Selected option value |

---

## Differences from get_worksheet_data

| Aspect | `get_consolidated_worksheet_data` | `get_worksheet_data` |
| ------ | --------------------------------- | -------------------- |
| Parameter | `form_id` (all versions) | `correlation_id` (one form version) |
| Version loop | Yes — all versions from metadata | No loop — single version |
| Version gating | Yes — per-field NULL for other versions | Not applicable |
| Extra output column | `form_version_label` | Not present |
| Worksheet scope | Per-(version, worksheet) | Per-worksheet for one version |

---

## Differences from get_consolidated_formversion_data

| Aspect | `get_consolidated_worksheet_data` | `get_consolidated_formversion_data` |
| ------ | --------------------------------- | ------------------------------------ |
| Data source | `Flex.WorksheetInstances` | `ApplicationFormSubmissions` |
| Implementation | CTE-based SQL | Procedural PL/pgSQL loops |
| Dual-schema | No (single worksheet JSON schema) | Yes (legacy + current CHEFS schema) |
| Worksheet identity | Worksheet name from DataPath | Not applicable |

---

## Debugging Queries

```sql
-- Check versions and worksheet links stored in mapping metadata
SELECT kv.key, kv.value
FROM "Reporting"."ReportColumnsMaps" rcm,
     jsonb_each_text(rcm."Mapping"->'Metadata'->'Info') kv
WHERE rcm."Id" = '<report_map_id>';

-- Check VersionLabels in the mapping
SELECT row_data->>'ColumnName', row_data->>'VersionLabel', row_data->>'DataPath'
FROM "Reporting"."ReportColumnsMaps" rcm,
     jsonb_array_elements(rcm."Mapping"->'Rows') row_data
WHERE rcm."Id" = '<report_map_id>';

-- Inspect worksheet instances linked to a form (via WorksheetLinkCorrelationId matching form version IDs)
SELECT wi."Id", wi."WorksheetId", wi."CorrelationId", w."Name"
FROM "Flex"."WorksheetInstances" wi
JOIN "Flex"."Worksheets" w ON wi."WorksheetId" = w."Id"
LIMIT 10;

-- Test function output
SELECT "Reporting".get_consolidated_worksheet_data('<form_id>', '<report_map_id>');
```
