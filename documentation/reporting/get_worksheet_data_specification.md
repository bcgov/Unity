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
- **Currency**: Validates numeric format, converts to DECIMAL(10,2)
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
- Parses JSON array from stored string
- Extracts individual checkbox values by key
- Returns boolean values for each checkbox option

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
| Currency | DECIMAL(10,2) | NULL::DECIMAL(10,2) |
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

## Related Documentation
- [Worksheet Schema Parser](./worksheet_field_schema_parser.md)
- [Report Column Mapping](./report_column_mapping.md)
- [Field Type Definitions](./field_type_definitions.md)