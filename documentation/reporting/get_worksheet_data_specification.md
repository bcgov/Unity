# get_worksheet_data.sql Function Specification

## Overview
The `get_worksheet_data` function is a PostgreSQL PL/pgSQL function that dynamically generates SQL queries to extract worksheet data for reporting purposes. It handles multiple worksheet types, field types, and data structures including both root-level fields and DataGrid components.

## Function Signature
```sql
CREATE OR REPLACE FUNCTION "Reporting".get_worksheet_data(
    correlation_id uuid, 
    report_map_id uuid
) 
RETURNS text
LANGUAGE plpgsql
```

## Purpose
- Generates dynamic SQL queries based on report column mappings
- Extracts worksheet data from JSONB structures
- Handles complex field types (DataGrids, Checkbox Groups, Radio Groups)
- Supports multiple worksheets and field configurations in a single query
- Returns a formatted SQL query string that can be executed to retrieve reporting data

## Input Parameters
- **correlation_id**: UUID identifying the worksheet instance correlation
- **report_map_id**: UUID identifying the report column mapping configuration

## Return Value
Returns a TEXT string containing a complete SQL query that can be executed to retrieve worksheet data formatted for reporting.

## Data Sources
The function queries the following tables:
- `"Reporting"."ReportColumnsMaps"`: Contains column mapping configurations
- `"Flex"."WorksheetInstances"`: Contains worksheet instance data
- `"Flex"."Worksheets"`: Contains worksheet metadata

## Core Processing Logic

### 1. Mapping Data Extraction (mapping_data CTE)
Extracts column mapping information from the ReportColumnsMaps table:
- **ColumnName**: Report column identifier
- **Type**: Field type (checkbox, radio, text, etc.)
- **DataPath**: Path to data in JSON structure
- **TypePath**: Hierarchical type information
- **Path**: Field path within worksheet structure

### 2. Unique Mappings (unique_mappings CTE)
- Uses `DISTINCT ON (column_name)` to ensure each column gets only one mapping
- Extracts worksheet name and data grid information
- Splits DataPath to identify DataGrid and field names

### 3. Worksheet Categorization
**DataGrid Fields (unique_worksheet_datagrids CTE)**:
- Identifies fields within DataGrid components
- Groups by worksheet and DataGrid name

**Root Level Fields (unique_worksheets_with_root CTE)**:
- Identifies fields at the worksheet root level (non-DataGrid)

### 4. Query Generation

#### DataGrid Queries (datagrid_queries CTE)
Generates queries for DataGrid field extraction:
- Iterates through DataGrid rows using `jsonb_array_elements`
- Creates row identifiers: `{datagrid_name}_r{row_number}`
- Handles all column types with appropriate DataGrid cell extraction

#### Root Queries (root_queries CTE)
Generates queries for root-level field extraction:
- Single row per worksheet with identifier: `'root'`
- Extracts values directly from worksheet CurrentValue structure

## Field Type Handling

### Simple Field Types
- **Text**: Direct string extraction
- **Currency**: Strips thousands-separator commas and surrounding whitespace (e.g. `1,470.07` → `1470.07`), validates numeric format, converts to DECIMAL(18,2)
- **Number**: Validates numeric format, converts to NUMERIC
- **Date**: Validates and converts to TIMESTAMP

### Complex Field Types

#### Regular Checkbox
**Data Structure**: `{"key": "Field9", "value": "true"}`
**Processing**: Converts text values to boolean using predefined value lists
- True values: 'true', 't', '1', 'yes', 'on'
- False values: 'false', 'f', '0', 'no', 'off', ''

#### Checkbox Group
**Data Structure**: `{"key": "Field10", "value": "[{\"key\":\"check1\",\"value\":false},{\"key\":\"check2\",\"value\":true}]"}`
**Processing**:
- Casts the stored string with `Reporting.safe_to_jsonb(...)` and guards on `jsonb_typeof(...) = 'array'`
- Only when that guard passes does it parse the array and extract the option matching the second segment of the DataPath
- Anything else — NULL, `''`, or a non-array value — yields `NULL` for that column

```sql
CASE WHEN jsonb_typeof("Reporting".safe_to_jsonb(<stored value>)) = 'array'
     THEN (SELECT (checkbox_elem->>'value')::BOOLEAN
           FROM jsonb_array_elements("Reporting".safe_to_jsonb(<stored value>)) AS checkbox_elem
           WHERE checkbox_elem->>'key' = '<option key>')
     ELSE NULL END
```

> **Why the guard exists (AB#33799).** A checkbox-group field saved with zero selections could persist an empty string rather than null. A bare `::jsonb` cast on that value raised `invalid input syntax for type json` and broke the *entire* generated view, not just the one column. The guard makes a bad value a NULL cell instead of a dead view.

#### Radio Group
**Data Structure**: `{"key": "Field12", "value": "Radio1"}`
**Processing**: Returns the selected option value as text (not boolean)

## Generated Query Structure

The final output is a UNION query combining:
1. All DataGrid queries (one per worksheet-DataGrid combination)
2. All root field queries (one per worksheet with root fields)

### Output Columns
- **worksheet_instance_id**: Worksheet instance identifier
- **application_id**: Correlation ID
- **worksheet_name**: Name of the worksheet
- **row_identifier**: Row identifier ('root' for root fields, '{datagrid}_r{n}' for DataGrid rows)
- **Dynamic columns**: All mapped report columns with appropriate data types

## Error Handling
- NULL handling for missing or invalid data
- Type conversion with fallback to NULL for invalid formats
- JSON parsing errors gracefully handled
- Missing worksheet or field data returns NULL values

## Path Parsing Logic

### DataPath Format
- **Standard**: `"(WorksheetName)FieldName"`
- **DataGrid**: `"(WorksheetName)DataGridName->FieldName"`
- **Checkbox Group**: `"(WorksheetName)FieldName->OptionName"`

### TypePath Format
- **Root Field**: `"worksheet->section->fieldtype"`
- **DataGrid Field**: `"worksheet->section->datagrid->fieldtype"`
- **Checkbox Group**: `"worksheet->section->checkboxgroup->Checkbox"`

## Data Type Mapping
| Field Type | SQL Type | NULL Type |
|------------|----------|-----------|
| Currency | DECIMAL(18,2) | NULL::DECIMAL(18,2) |
| Number | NUMERIC | NULL::NUMERIC |
| Date | TIMESTAMP | NULL::TIMESTAMP |
| Checkbox | BOOLEAN | NULL::BOOLEAN |
| Radio | TEXT | NULL::TEXT |
| Default | TEXT | NULL::TEXT |

## Performance Considerations
- Uses DISTINCT ON for deduplication
- Leverages JSONB operators for efficient JSON parsing
- Generates optimized column lists with appropriate NULL handling
- Orders results by worksheet_name and row_identifier

## Common Use Cases

### 1. Single Worksheet with Root Fields Only
- Generates one query with 'root' row identifier
- All fields extracted from CurrentValue->values array

### 2. Single Worksheet with DataGrid Only
- Generates one query per DataGrid
- Multiple rows per DataGrid based on data
- Row identifiers: DataGridName_r1, DataGridName_r2, etc.

### 3. Multiple Worksheets Mixed Fields
- Generates separate queries for each worksheet-DataGrid combination
- Generates separate queries for worksheets with root fields
- All combined with UNION ALL

### 4. Complex Field Types
- Checkbox groups parsed from JSON arrays
- Radio fields return actual selected values
- Regular checkboxes converted to boolean

## Debugging Tips

### Common Issues
1. **Missing Data**: Check if TypePath correctly identifies field location
2. **Wrong Data Type**: Verify column_type matching in mapping
3. **JSON Parse Errors**: Check DataPath format and clean_data_path extraction
4. **Duplicate Columns**: Ensure DISTINCT ON is working with proper ordering

### Debugging Queries
```sql
-- Check mapping data
SELECT * FROM "Reporting"."ReportColumnsMaps" WHERE "Id" = 'your-report-map-id';

-- Check worksheet instances
SELECT * FROM "Flex"."WorksheetInstances" 
WHERE "WorksheetCorrelationId" = 'your-correlation-id';

-- Test function output
SELECT "Reporting".get_worksheet_data('correlation-id', 'report-map-id');
```

## Version History
- **v1.0**: Initial implementation with basic field types
- **v1.1**: Added DataGrid support
- **v1.2**: Added checkbox group support
- **v1.3**: Fixed radio field handling to return text values
- **v1.4**: Enhanced error handling and NULL type consistency
- **v1.5**: Increased currency precision from `DECIMAL(10,2)` to `DECIMAL(18,2)`.
- **v1.6**: Locale-formatted currency values are normalized by stripping commas and whitespace before numeric validation, so values such as `1,470.07` are persisted instead of becoming `NULL`. Paired with the `WorksheetFieldSchemaParser` mixed-DataGrid fix, which now emits mapping rows for statically-defined columns on mixed (dynamic + static) grids so the SQL function actually receives mappings to project. This version also introduced `Reporting.safe_to_jsonb(...)` for safe JSONB casting.
- **v1.7** *(AB#33799 / AB#33877, Aug 2026)*: Hardened checkbox-group extraction. A stored value that is null, `''`, or otherwise not a JSON array now resolves to `NULL` for that column instead of raising `invalid input syntax for type json` and breaking the whole view. Shipped in two steps — an initial `~ '^\[.*\]$'` regex guard, then replaced on CodeQL feedback with `jsonb_typeof("Reporting".safe_to_jsonb(...)) = 'array'`, which is what the current file contains. Deployed by tenant migration `20260806201536_AB33799_HardenCheckboxGroupReportingViews` (re-runs this script and `get_consolidated_worksheet_data.sql`; `CREATE OR REPLACE` makes it idempotent), with a companion one-off data backfill in `20260806202244_AB33799_FixCheckboxGroupEmptyValues` that rewrites the already-persisted empty-string values to JSON `null`.

> **Migration references.** Earlier versions of this document cited per-feature migrations (`20251125234153_UpdateViewGenCurrencyPrecision`, `20260416000002_AddSafeToJsonbAndGuardDataGridLateral`). Those were squashed into `20260721203242_Initial` and no longer exist. The `Initial` migration deploys the current state of every script in `Scripts/`; only the two AB#33799 migrations above post-date it.