# Database Scripts

This folder contains SQL scripts that are embedded as resources and used by Entity Framework migrations.

## Files

### `get_formversion_data.sql`
PostgreSQL function that generates dynamic SELECT clauses for form version data extraction. Used by the `generate_formversion_view` procedure to create database views from form submission data.

### `generate_formversion_view.sql`
PostgreSQL stored procedure that creates database views for form version reporting. This procedure:
- Validates form version mapping configuration
- Generates view names automatically if not provided
- Creates dynamic SQL views based on form field mappings
- Handles both root fields and dataGrid fields

### `get_worksheet_data.sql`
PostgreSQL function that generates dynamic SELECT clauses for worksheet data extraction. Used by the `generate_worksheet_view` procedure to create database views from Flex worksheet instance data. Handles:
- Root fields and datagrid fields from worksheet instances
- Type conflicts and fallback handling
- Complex JSON path extraction from worksheet CurrentValue

### `generate_worksheet_view.sql`
PostgreSQL stored procedure that creates database views for worksheet reporting. This procedure:
- Validates worksheet mapping configuration
- Generates view names automatically if not provided  
- Creates dynamic SQL views based on worksheet field mappings
- Handles worksheet correlation IDs

### `get_scoresheet_data.sql`
PostgreSQL function that generates dynamic SELECT clauses for scoresheet data extraction. Used by the `generate_scoresheet_view` procedure to create database views from Flex scoresheet instance data. Features:
- Extracts data from scoresheet ReportData JSONB column
- Falls back to Answers table via JOIN when needed
- Handles assessment correlation through Assessments table
- Includes TotalScore calculation from ReportData
- Supports various field types (textfield, number, currency, yesno, checkbox, radio, etc.)

### `generate_scoresheet_view.sql`
PostgreSQL stored procedure that creates database views for scoresheet reporting. This procedure:
- Validates scoresheet mapping configuration
- Generates view names automatically if not provided
- Creates dynamic SQL views based on scoresheet field mappings
- Links scoresheet instances to assessments and applications

### `calculate_scoresheet_total_score.sql`
PostgreSQL function that calculates the total score for a scoresheet instance by:
- Iterating through all answers for the given scoresheet instance
- Applying scoring logic based on question type:
  - **Number (1)**: Uses the numeric answer value directly
  - **YesNo (6)**: Applies yes_value or no_value from question definition based on response
  - **SelectList (12)**: Applies numeric_value from the selected option in question definition
  - **Text (2) & TextArea (14)**: No score contribution
- Returns the calculated total score as a NUMERIC value

## Embedded Resource Configuration

These SQL files are configured as **Embedded Resources** in the project file:

```xml
<ItemGroup>
  <EmbeddedResource Include="Scripts\get_formversion_data.sql" />
  <EmbeddedResource Include="Scripts\generate_formversion_view.sql" />
  <EmbeddedResource Include="Scripts\get_worksheet_data.sql" />
  <EmbeddedResource Include="Scripts\generate_worksheet_view.sql" />
  <EmbeddedResource Include="Scripts\get_scoresheet_data.sql" />
  <EmbeddedResource Include="Scripts\generate_scoresheet_view.sql" />
  <EmbeddedResource Include="Scripts\calculate_scoresheet_total_score.sql" />
</ItemGroup>
```

## Migration Dependencies

### ⚠️ **CRITICAL MIGRATION NOTE**

The SQL scripts in this folder are deployed via Entity Framework migrations:
- **Migration**: `20251010193253_AddFormVersionViewGen.cs`
- **Migration**: `20251113012409_AddWorksheetViewGeneration.cs` 
- **Migration**: `20251113001650_AddScoresheetViewGeneration.cs`
- **Resource Names**: 
  - `Unity.GrantManager.Scripts.get_formversion_data.sql`
  - `Unity.GrantManager.Scripts.generate_formversion_view.sql`
  - `Unity.GrantManager.Scripts.get_worksheet_data.sql`
  - `Unity.GrantManager.Scripts.generate_worksheet_view.sql`
  - `Unity.GrantManager.Scripts.get_scoresheet_data.sql`
  - `Unity.GrantManager.Scripts.generate_scoresheet_view.sql`
  - `Unity.GrantManager.Scripts.calculate_scoresheet_total_score.sql`

### **If Migration History is Cleaned Up**

When cleaning up migration history or squashing migrations, you **MUST** ensure these SQL scripts are deployed. This can be done by:

1. **Creating a new migration** that includes these scripts:
   ```csharp
   // In the new migration's Up() method
   var assembly = Assembly.GetExecutingAssembly();
   var resourceName = "Unity.GrantManager.Scripts.get_formversion_data.sql";
   using Stream stream = assembly.GetManifestResourceStream(resourceName) 
       ?? throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");
   using StreamReader reader = new StreamReader(stream);
   string sql = reader.ReadToEnd();
   migrationBuilder.Sql(sql);
   
   // Repeat for all other .sql files
   ```

2. **Or manually deploying** the scripts to the database before running the application

### Resource Name Pattern

Embedded resources follow this naming convention:
```
{RootNamespace}.{FolderPath}.{FileName}
```

For this project:
- **RootNamespace**: `Unity.GrantManager` (from .csproj)
- **FolderPath**: `Scripts`
- **Result**: `Unity.GrantManager.Scripts.{filename}.sql`

## Usage

These scripts are automatically deployed when running Entity Framework migrations. The application uses them for:

- **Form Version Reporting**: Creating dynamic database views from CHEFS form submissions
- **Worksheet Reporting**: Creating dynamic database views from Flex worksheet instances  
- **Scoresheet Reporting**: Creating dynamic database views from Flex scoresheet instances
- **Data Transformation**: Converting JSON form/worksheet/scoresheet data into structured relational views
- **Performance**: Pre-computed views for faster reporting queries

## Data Flow

### Scoresheet Data Flow
1. **ScoresheetInstances**: Contains scoresheet answers in `ReportData` JSONB column
2. **Answers**: Individual answers linked to Questions via QuestionId
3. **Questions**: Scoresheet questions with metadata
4. **Assessments**: Links scoresheet instances to applications via CorrelationId
5. **Applications**: The main application entity

### Key Relationships
- `ScoresheetInstances.CorrelationId` → `Assessments.Id`
- `Assessments.ApplicationId` → `Applications.Id` 
- `ScoresheetInstances.ScoresheetId` → `Scoresheets.Id`
- `Answers.ScoresheetInstanceId` → `ScoresheetInstances.Id`
- `Answers.QuestionId` → `Questions.Id`

## Dependencies

- **PostgreSQL 12+**: Uses advanced JSONB operations and dynamic SQL
- **Entity Framework Core**: Deployed via migrations
- **ABP Framework**: Used within the reporting module context
- **Flex Module**: Required for worksheet and scoresheet functionality