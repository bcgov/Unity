# get_scoresheet_data.sql Function Specification

## Overview

`get_scoresheet_data` is a PostgreSQL PL/pgSQL function that generates a dynamic SQL query for extracting Unity.Flex scoresheet answer data. It is called by the `generate_scoresheet_view` stored procedure. Unlike the form version and worksheet equivalents, scoresheet field values are read from the normalised `Flex.Answers` table via a correlated subquery per field — not from a JSON blob on the scoresheet instance itself.

## Function Signature

```sql
CREATE OR REPLACE FUNCTION "Reporting".get_scoresheet_data(
    correlation_id uuid,
    report_map_id uuid
)
RETURNS text
LANGUAGE plpgsql
```

## Purpose

- Generates a dynamic SQL SELECT query for scoresheet instance data
- Reads field values from `Flex.Answers` joined to `Flex.Questions` (one correlated subquery per mapped field)
- Always includes a `total_score` column calculated via `calculate_scoresheet_total_score()`
- Handles scoresheet-specific field types (`number`, `yesno`, text variants)
- No DataGrid support — scoresheets are flat (one row per scoresheet instance)
- Returns a complete SQL query string ready for use as a view body

## Input Parameters

- **correlation_id** — UUID of the application form (`ApplicationFormId`). Used to filter applications linked to scoresheet instances.
- **report_map_id** — UUID of the `ReportColumnsMaps` record containing the field-to-column mapping configuration.

## Return Value

Returns a `TEXT` string containing a complete SQL SELECT query.

---

## Data Sources

| Table | Schema | Purpose |
| ----- | ------ | ------- |
| `ScoresheetInstances` | `Flex` | One row per assessor's completed scoresheet; CorrelationId → Assessment |
| `Assessments` | *(public)* | Links scoresheet instances to applications via `ApplicationId` |
| `Applications` | *(public)* | Application record; filtered by `ApplicationFormId = correlation_id` |
| `Answers` | `Flex` | Normalised answer rows; one per question per scoresheet instance |
| `Questions` | `Flex` | Question definitions including `Name` used to match answers |

### Join path

```
ScoresheetInstances (si)
  JOIN Assessments (a)   ON si."CorrelationId" = a."Id"
  JOIN Applications (ap) ON a."ApplicationId"  = ap."Id"
```

Filtered by: `ap."ApplicationFormId" = correlation_id`

---

## Field Value Extraction

Each mapped field's value is retrieved via a **correlated subquery** embedded directly in the SELECT clause:

```sql
(
  SELECT a_1."CurrentValue"->>'value'
  FROM "Flex"."Answers" a_1
  JOIN "Flex"."Questions" q ON a_1."QuestionId" = q."Id"
  WHERE a_1."ScoresheetInstanceId" = si."Id"
    AND q."Name" = '<field_key>'
  LIMIT 1
)
```

The `<field_key>` value is resolved using the following priority:

1. **Key** (`row_data->>'Key'`) — used if non-empty
2. **PropertyName** (`row_data->>'PropertyName'`) — used if Key is empty
3. **DataPath** (`row_data->>'DataPath'`) — used if PropertyName is also empty
4. **ColumnName** — final fallback

---

## Core Processing Logic

### Pass 1 — Column inventory and type-conflict detection

Iterates all mapping rows to collect unique column names and their types (normalised to lowercase). Detects type conflicts (same column name with different types) and marks them for TEXT fallback.

### Pass 2 — NULL column list construction

Builds a typed NULL placeholder list ordered by column name, using scoresheet-specific types:

- `number` → `NULL::NUMERIC`
- `yesno` → `NULL::BOOLEAN`
- All other types → `NULL::TEXT`

### Pass 3 — Query construction

Initialises a base SELECT with fixed columns, then iterates mapping rows to replace each NULL placeholder with the appropriate correlated subquery + type cast.

### Final query shape

```sql
SELECT
    si."Id"                                    AS scoresheet_instance_id,
    ap."Id"                                    AS application_id,
    si."CorrelationId"                         AS assessment_id,
    si."ScoresheetId"                          AS scoresheet_id,
    "Reporting".calculate_scoresheet_total_score(si."Id") AS total_score,
    <field_subquery>::<type>                   AS <column_name>,
    ...
FROM "Flex"."ScoresheetInstances" si
JOIN "Assessments" a  ON si."CorrelationId" = a."Id"
JOIN "Applications" ap ON a."ApplicationId" = ap."Id"
WHERE ap."ApplicationFormId" = '<correlation_id>'
```

---

## Fixed Output Columns

Every generated query includes these columns regardless of the field mapping:

| Column | Source | Description |
| ------ | ------ | ----------- |
| `scoresheet_instance_id` | `si."Id"` | UUID of the scoresheet instance |
| `application_id` | `ap."Id"` | UUID of the parent application |
| `assessment_id` | `si."CorrelationId"` | UUID of the assessment linking instance to application |
| `scoresheet_id` | `si."ScoresheetId"` | UUID of the scoresheet definition |
| `total_score` | `calculate_scoresheet_total_score(si."Id")` | Computed total score for the instance |

---

## Field Type Handling

Scoresheet question types differ from Forms.io types. The function normalises all types to lowercase before comparison.

| Type | SQL Type | Extraction |
| ---- | -------- | ---------- |
| `number` | NUMERIC | Numeric regex validation → cast, or NULL |
| `yesno` | BOOLEAN | `true/t/1/yes` → true, `false/f/0/no` → false, else NULL |
| `text` / `textarea` / `selectlist` / *(any other)* | TEXT | Direct string value |
| *(type conflict fallback)* | TEXT | Cast to TEXT |

---

## No DataGrid Support

Scoresheets do not have DataGrid (repeating row) components. The function generates a single flat query — one output row per scoresheet instance. There is no `row_identifier` column.

---

## calculate_scoresheet_total_score

This companion function (`Reporting.calculate_scoresheet_total_score(instance_id)`) is always called as part of the generated query. It iterates all answers for the given instance and applies scoring logic:

- **Number questions**: uses the numeric answer value directly
- **YesNo questions**: applies the `yes_value` or `no_value` from the question definition
- **SelectList questions**: applies the `numeric_value` from the selected option
- **Text / TextArea questions**: contributes 0 to the total

---

## Data Type Mapping

| Question Type | Normal SQL Type | Type-Conflict SQL Type |
| ------------- | --------------- | ---------------------- |
| `number` | NUMERIC | TEXT |
| `yesno` | BOOLEAN | TEXT |
| `text` / `textarea` / `selectlist` / others | TEXT | TEXT |

---

## Differences from get_worksheet_data and get_formversion_data

| Aspect | `get_scoresheet_data` | `get_worksheet_data` | `get_formversion_data` |
| ------ | --------------------- | -------------------- | ---------------------- |
| Data source | `Flex.Answers` (normalised rows) | `Flex.WorksheetInstances` (JSON blob) | `ApplicationFormSubmissions` (JSON blob) |
| Field extraction | Correlated subquery per field | JSONB path expression | JSONB path expression |
| Total score | Always included | Not applicable | Not applicable |
| DataGrid support | No | Yes | Yes |
| Row identifier | Not present | `root` or `{grid}_r{n}` | `root` or `{grid}_r{n}` |
| Types | `number`, `yesno`, text | CHEFS types | CHEFS types |
| Correlation ID | Form ID | Form Version ID | Form Version ID |

---

## Common Use Cases

### 1. Standard scoresheet with number and text questions

Generates one row per scoresheet instance with NUMERIC and TEXT columns, plus `total_score`.

### 2. YesNo questions (boolean)

`yesno` fields are extracted as BOOLEAN using a simple true/false lookup.

### 3. SelectList questions

`selectlist` fields are returned as TEXT (the selected option's label or value).

---

## Debugging Queries

```sql
-- Check the mapping configuration
SELECT "Mapping"->'Rows' FROM "Reporting"."ReportColumnsMaps" WHERE "Id" = '<report_map_id>';

-- List scoresheet instances linked to a form
SELECT si."Id", si."ScoresheetId", si."CorrelationId", ap."Id" AS application_id
FROM "Flex"."ScoresheetInstances" si
JOIN "Assessments" a ON si."CorrelationId" = a."Id"
JOIN "Applications" ap ON a."ApplicationId" = ap."Id"
WHERE ap."ApplicationFormId" = '<correlation_id>'
LIMIT 10;

-- Inspect answers for a scoresheet instance
SELECT q."Name", a."CurrentValue"->>'value' AS answer_value
FROM "Flex"."Answers" a
JOIN "Flex"."Questions" q ON a."QuestionId" = q."Id"
WHERE a."ScoresheetInstanceId" = '<instance_id>';

-- Test function output
SELECT "Reporting".get_scoresheet_data('<correlation_id>', '<report_map_id>');

-- Test total score calculation
SELECT "Reporting".calculate_scoresheet_total_score('<scoresheet_instance_id>');
```
