/*
 * Example Usage of the Forms.io Mapping and View Generation Functions
 * 
 * This file demonstrates how to use the new Forms.io mapping functionality
 * to generate database views from form field mappings.
 */

/*
1. **Creating a Column Mapping from Forms.io Data**

Given the Forms.io mapping JSON structure you provided:
```json
{
  "Rows": [
    {
      "Type": "textfield",
      "Label": "MailingStreet1", 
      "Parent": false,
      "ColumnName": "mailingstree",
      "PropertyName": "mailingAddress1"
    },
    {
      "Type": "select",
      "Label": "Select List",
      "Parent": false,
      "ColumnName": "select_list", 
      "PropertyName": "select"
    },
    {
      "Type": "panel",
      "Label": "Panel",
      "Parent": true,
      "ColumnName": "panel",
      "PropertyName": "panel"
    }
  ]
}
```

The system will:
- Parse each field type and map it to appropriate PostgreSQL data types
- Generate column definitions for the database view
- Skip parent fields (containers) that don't hold actual data
- Create mock data rows for initial testing

2. **Database Function Usage**

The system creates a PostgreSQL stored procedure: `"Reporting".generate_formversion_view(correlation_id UUID, correlation_provider TEXT)`

This function:
- Reads the mapping from ReportColumnsMaps table using correlationId and correlationProvider
- Extracts the ViewName and Mapping JSON data  
- Parses the mapping rows to understand field types and column names
- Generates appropriate PostgreSQL column types based on Forms.io field types
- Creates a database view with the specified columns and mock data
- Drops existing views with the same name to avoid conflicts

3. **Field Type Mappings**

Forms.io Type -> PostgreSQL Type:
- textfield, textarea, email, select, phonenumber -> TEXT
- number, currency -> NUMERIC  
- datetime, day -> TIMESTAMP
- option, checkbox, radio -> BOOLEAN
- Unknown types -> TEXT (default)

4. **API Usage Examples**

// Check if a view name is available
bool isAvailable = await columnsMappingService.IsViewNameAvailableAsync("my_form_view");

// Generate a view (queues background job)
string result = await columnsMappingService.GenerateViewAsync(
    correlationId, 
    "chefs", // or your correlation provider
    "my_form_view"
);

// Check if view exists after generation
bool viewExists = await columnsMappingService.ViewExistsAsync("my_form_view");

// Get view data with pagination
var request = new ViewDataRequest 
{
    Skip = 0,
    Take = 100,
    Filter = "column_name IS NOT NULL", // Optional SQL WHERE clause
    OrderBy = "column_name ASC" // Optional SQL ORDER BY clause
};

ViewDataResult data = await columnsMappingService.GetViewDataAsync("my_form_view", request);

// Get column information
string[] columns = await columnsMappingService.GetViewColumnNamesAsync("my_form_view");

5. **Background Job Processing**

The view generation is handled asynchronously via a background job:
- GenerateViewBackgroundJob processes the actual database view creation
- Uses the stored procedure to generate the view
- Handles tenant context properly for multi-tenant scenarios
- Logs success/failure for monitoring

6. **Mock Data Generation**

For initial testing, the system generates mock data based on field types:
- textfield: 'Sample [fieldname]'
- email: 'sample@example.com'
- phonenumber: '(555) 123-4567'
- number: 123.45
- currency: 1234.56
- datetime: '2024-01-01 12:00:00'
- checkbox/option: true
- select: 'Option 1 for [fieldname]'

7. **Error Handling**

The system handles various error scenarios:
- Missing correlation mappings
- Invalid view names
- Database connection issues
- JSON parsing errors
- Field type validation errors

8. **Utility Functions Available**

FormsIoMappingUtils class provides:
- MapToPostgreSqlType(string formsIoType): Maps field types to SQL types
- GenerateMockData(string formsIoType, string fieldName): Creates test data
- IsParentField(string formsIoType): Identifies container fields
- IsSupportedFieldType(string formsIoType): Validates field types
- GetFieldTypeDescription(string formsIoType): Gets human-readable descriptions
- ExtractMappingRows(ReportColumnsMap): Parses JSON mapping data

9. **Sample View Result**

Based on your example data, the generated view would look like:
```sql
CREATE VIEW "Reporting"."my_form_view" (
    mailingstree TEXT,
    mailingstreet2 TEXT,
    mailingaddresscity TEXT,
    select_list TEXT,
    phone_number TEXT
    -- parent fields like "panel" are excluded
) AS SELECT 
    'Sample mailingstree',
    'Sample mailingstreet2', 
    'Sample mailingaddresscity',
    'Option 1 for select_list',
    '(555) 123-4567'
```

10. **Next Steps for Production**

Currently the views contain mock data. To make this production-ready:
- Update the stored procedure to query actual submission data
- Implement JSON field extraction from form submissions
- Add data type conversion and validation
- Handle nested/complex field structures
- Add proper indexing for performance
*/