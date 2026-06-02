# get_formversion_data.sql Function Specification

## Overview

`get_formversion_data` is a PostgreSQL PL/pgSQL function that generates a dynamic SQL query for extracting CHEFS form submission data from a specific form version. It is called by the `generate_formversion_view` stored procedure to create flat, queryable database views from raw CHEFS JSON submission blobs.

## Function Signature

```sql
CREATE OR REPLACE FUNCTION "Reporting".get_formversion_data(
    correlation_id uuid,
    report_map_id uuid
)
RETURNS text
LANGUAGE plpgsql
```

## Purpose

- Generates a dynamic SQL SELECT query based on the report column mappings for a form version
- Extracts individual field values from CHEFS JSON submission data
- Handles both root fields and DataGrid (repeating row) fields
- Supports the two CHEFS submission JSON schemas (legacy and current) via UNION ALL
- Detects type conflicts and applies TEXT fallback for affected columns
- Returns a complete SQL query string ready for execution as a view body

## Input Parameters

- **correlation_id** — UUID of the form version (`ApplicationFormVersionId`). Used to filter `ApplicationFormSubmissions`.
- **report_map_id** — UUID of the `ReportColumnsMaps` record containing the field-to-column mapping configuration.

## Return Value

Returns a `TEXT` string containing a complete SQL query (or UNION ALL of queries) that can be executed or used as a view definition.

---

## Dual-Schema Design

CHEFS form submissions are stored in two different JSON structures depending on when the submission was created. The function detects which schema applies to each row and generates queries for both, combined with `UNION ALL`.

### Legacy schema

Used for older submissions where `submission->'submission'->'submission'` **IS NULL**:

```sql
afs."Submission"->'submission'->'data'->'fieldKey'
```

### Current schema

Used for newer submissions where `submission->'submission'->'submission'` **IS NOT NULL**:

```sql
afs."Submission"->'submission'->'submission'->'data'->'fieldKey'
```

### Detection

Each generated query pair is split into two branches combined with `UNION ALL`:

```sql
(SELECT ... FROM ApplicationFormSubmissions afs
 WHERE afs."ApplicationFormVersionId" = '<id>'
   AND afs."Submission"->'submission'->'submission' IS NULL)      -- legacy
UNION ALL
(SELECT ... FROM ApplicationFormSubmissions afs
 WHERE afs."ApplicationFormVersionId" = '<id>'
   AND afs."Submission"->'submission'->'submission' IS NOT NULL)  -- current
```

Both branches select identical columns so their types are always consistent across the UNION.

---

## Core Processing Logic

### Pass 1 — Column inventory and type-conflict detection

Iterates all mapping rows to:
- Collect unique column names and their declared types
- Detect **type conflicts**: if the same column name appears in two rows with different types, the column is flagged for TEXT fallback (`use_text_fallback = true`)
- Identify DataGrid fields (TypePath contains `'datagrid'`) and collect unique DataGrid names
- Identify root-level fields (TypePath does not contain `'datagrid'`)

### Pass 2 — NULL column list construction

Builds a typed NULL placeholder list ordered by column name, used as a consistent column template in all UNION ALL branches:

```sql
NULL::TEXT AS first_name, NULL::NUMERIC AS budget, NULL::BOOLEAN AS consent_given
```

This ensures all UNION ALL branches have identical column sets and types.

### Pass 3 — Query generation

For each unique DataGrid and for root fields, generates a UNION ALL pair (legacy + current) substituting NULL placeholders with actual JSON extraction expressions.

---

## Data Sources

- `public."ApplicationFormSubmissions"` — contains the raw CHEFS submission JSON and the `ApplicationFormVersionId` foreign key

---

## Field Type Handling

| Type | SQL Type | Extraction |
| ---- | -------- | ---------- |
| `textfield` / `textarea` / `email` / `select` / `phoneNumber` | TEXT | `->>` operator on JSON path |
| `number` | NUMERIC | Numeric regex validation → cast, or NULL |
| `currency` | DECIMAL(18,2) | Numeric regex validation → cast, or NULL |
| `option` / `checkbox` | BOOLEAN | `true/t/1/yes` → true, `false/f/0/no` → false, else NULL |
| *(type conflict fallback)* | TEXT | All values cast to TEXT with type-aware formatting |

### Type conflict TEXT fallback

When a column has conflicting types across mapping rows, the function converts values to TEXT and emits them as strings rather than typed values. This prevents UNION ALL type mismatches in the generated view.

---

## DataGrid Handling

DataGrid fields are identified by TypePath containing `'datagrid'`. For each unique DataGrid:

1. A `CROSS JOIN LATERAL jsonb_array_elements(...)` expands the DataGrid JSON array into individual rows
2. Row numbers are tracked with `row_number() OVER()` to generate row identifiers: `{datagrid_name}_r{n}`
3. A separate UNION ALL pair (legacy + current) is generated for each DataGrid

DataPath format for DataGrid fields: `(DKn)dataGridKey->fieldKey` or `dataGridKey->fieldKey`.

---

## Output Columns

| Column | Source | Description |
| ------ | ------ | ----------- |
| `submission_id` | `afs."Id"` | UUID of the form submission record |
| `application_id` | `afs."ApplicationId"` | UUID of the parent application |
| `row_identifier` | `'root'` or `'{datagrid}_r{n}'` | `root` for flat fields; `{datagrid_name}_r{n}` for DataGrid rows |
| *(dynamic columns)* | Extracted from JSON | One column per mapped field, typed per mapping |

---

## Path Parsing Logic

### DataPath format

- **Root field**: `fieldKey` or `panel->fieldKey` (containers stripped)
- **DataGrid field**: `dataGridName->fieldName` or `(DK1)dataGridName->fieldName`

The function splits DataPath on `'->'` to build the JSONB navigation path. The final segment uses the `->>` text-extract operator; intermediate segments use `->`.

### DK prefix handling

If DataPath begins with `(DK{n})`, the prefix is stripped before extracting the DataGrid name, and the stripped path is used for navigation. The prefix itself is not used for data extraction.

---

## Data Type Mapping

| Field Type | Normal SQL Type | Type-Conflict SQL Type |
| ---------- | --------------- | ---------------------- |
| `textfield` / `textarea` / `email` / `select` / `phoneNumber` | TEXT | TEXT |
| `number` | NUMERIC | TEXT (as string representation) |
| `currency` | DECIMAL(18,2) | TEXT (as string representation) |
| `option` / `checkbox` | BOOLEAN | TEXT (`'true'` / `'false'`) |

---

## Relationship to `get_worksheet_data`

Both functions serve the same role (query generation), but differ in data source and schema:

| Aspect | `get_formversion_data` | `get_worksheet_data` |
| ------ | ---------------------- | -------------------- |
| Data source | `ApplicationFormSubmissions` | `Flex.WorksheetInstances` |
| JSON location | `submission->'submission'->'data'` (dual-schema) | `CurrentValue->'values'` array |
| Schema handling | Dual legacy/current UNION ALL | Single schema |
| Implementation style | Procedural PL/pgSQL | CTE-based SQL |
| Correlation ID | Form Version ID | Form Version ID |
| DataGrid support | Yes | Yes |

---

## Common Use Cases

### 1. Flat submission with root fields only

Generates two branches (legacy + current), each returning one row per submission with all field values as flat columns. `row_identifier` = `'root'` for all rows.

### 2. Submission with DataGrid fields

Generates a separate UNION ALL pair per DataGrid, expanding each DataGrid array entry into its own row. `row_identifier` = `'{datagrid_name}_r1'`, `'{datagrid_name}_r2'`, etc.

### 3. Mixed root and DataGrid fields

One root query pair + one query pair per DataGrid, all combined with `UNION ALL`. Root columns are NULL in DataGrid rows and vice versa.

### 4. Type conflict (field mapped with different types)

Affected columns fall back to TEXT for both value and NULL type, allowing the UNION ALL to be well-typed.

---

## Debugging Queries

```sql
-- Check the mapping configuration
SELECT "Mapping"->'Rows' FROM "Reporting"."ReportColumnsMaps" WHERE "Id" = '<report_map_id>';

-- Inspect raw submissions for a form version
SELECT "Id", "ApplicationId", "Submission"->'submission' FROM public."ApplicationFormSubmissions"
WHERE "ApplicationFormVersionId" = '<correlation_id>'
LIMIT 5;

-- Detect which schema variant a submission uses
SELECT "Id",
  CASE WHEN "Submission"->'submission'->'submission' IS NULL THEN 'legacy' ELSE 'current' END AS schema_version
FROM public."ApplicationFormSubmissions"
WHERE "ApplicationFormVersionId" = '<correlation_id>';

-- Test function output
SELECT "Reporting".get_formversion_data('<correlation_id>', '<report_map_id>');
```

---

## Version Notes

This function uses the same column-name generation and type-handling patterns as `get_worksheet_data`. Key differences are the dual-schema UNION ALL approach and the procedural (rather than CTE-based) implementation style.
