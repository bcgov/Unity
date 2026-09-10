# Database Scripts

This folder contains SQL scripts that are embedded as resources and used by Entity Framework migrations.

> These scripts split across the two reporting view-generation paths. The `get_*_data` functions and the singular `generate_{formversion,worksheet,scoresheet}_view` / `generate_consolidated_*_view` procedures belong to the **explicit** Reporting Configuration path. The plural `generate_{submissions,worksheets,scoresheets}_view` procedures belong to the **deprecated** auto/dynamic path. See `documentation/reporting/README.md`.

## Files — explicit path (Reporting Configuration)

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
- Extracts field values exclusively from the normalised `Flex.Answers` table (joined to `Flex.Questions`), one correlated subquery per mapped field — it does **not** read the `ScoresheetInstances.ReportData` JSONB column
- Handles assessment correlation through `Assessments` → `Applications`
- Always includes a `total_score` column computed by `calculate_scoresheet_total_score()`
- Supports various field types (textfield, number, currency, yesno, checkbox, radio, etc.)

### `get_consolidated_formversion_data.sql` / `get_consolidated_worksheet_data.sql`
Consolidated counterparts of `get_formversion_data` / `get_worksheet_data`. They merge fields across **all** versions of a form into a single SELECT, driven by a mapping whose CorrelationId is the Form ID rather than a version ID.

### `generate_consolidated_formversion_view.sql` / `generate_consolidated_worksheet_view.sql`
Stored procedures that create the consolidated views from the above functions, following the same validate → build → drop → create sequence as their per-version equivalents.

### `safe_to_date.sql` / `safe_to_timestamp.sql` / `safe_to_jsonb.sql`
Coercion helpers used inside the generated SELECT statements. Each returns `NULL` for input it cannot parse, so a single malformed value cannot fail an entire view.

## Files — deprecated auto/dynamic path

> Scheduled for removal. See `documentation/reporting/reporting-auto-generated-views.md` for the phased deprecation plan.

### `generate_submissions_view.sql` / `generate_worksheets_view.sql` / `generate_scoresheets_view.sql`
Three near-identical procedures that build a view of all-`TEXT` columns for a single form version, worksheet, or scoresheet. Each reads the pipe-delimited `ReportViewName` / `ReportColumns` / `ReportKeys` values off the definition row (`public.ApplicationFormVersion`, `Flex.Worksheets`, `Flex.Scoresheets`) and emits one `COALESCE(... jsonb_each_text("ReportData") ...)` column per key against the corresponding instance table. No mapping configuration is involved.

## Other

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

See the `<ItemGroup>` in `Unity.GrantManager.EntityFrameworkCore.csproj` for the authoritative list — every `.sql` file in this folder that a migration runs must have an entry there. As of the `20260721203242_Initial` tenant migration that is:

`safe_to_date`, `safe_to_timestamp`, `safe_to_jsonb`, `calculate_scoresheet_total_score`, `get_next_sequence_number`, `get_formversion_data`, `get_worksheet_data`, `get_scoresheet_data`, `get_consolidated_formversion_data`, `get_consolidated_worksheet_data`, `generate_formversion_view`, `generate_worksheet_view`, `generate_scoresheet_view`, `generate_consolidated_formversion_view`, `generate_consolidated_worksheet_view`, `generate_worksheets_view`, `generate_scoresheets_view`, `generate_submissions_view`, `unitydb-communities-script`.

## Migration Dependencies

### ⚠️ **CRITICAL MIGRATION NOTE**

The SQL scripts in this folder are deployed by the **tenant** migration `Migrations/TenantMigrations/20260721203242_Initial.cs`, which calls a `RunEmbeddedScript(migrationBuilder, "<filename>.sql")` helper once per script in dependency order (helpers → `get_*_data` functions → `generate_*` procedures) and drops each object again in its `Down()`.

The earlier per-feature migrations (`AddFormVersionViewGen`, `AddWorksheetViewGeneration`, `AddScoresheetViewGeneration`) were squashed into that `Initial` migration and no longer exist.

Resource names follow `Unity.GrantManager.Scripts.<filename>.sql` — see [Resource Name Pattern](#resource-name-pattern).

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

### Scoresheet Data Flow (explicit path — `get_scoresheet_data`)
1. **ScoresheetInstances**: One row per completed scoresheet; the anchor for the query
2. **Answers**: Individual answers linked to Questions via QuestionId — **this is where field values are read from**
3. **Questions**: Scoresheet questions with metadata
4. **Assessments**: Links scoresheet instances to applications via CorrelationId
5. **Applications**: The main application entity

The `ScoresheetInstances.ReportData` JSONB column belongs to the deprecated auto path (`generate_scoresheets_view`) and is not read by `get_scoresheet_data`.

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