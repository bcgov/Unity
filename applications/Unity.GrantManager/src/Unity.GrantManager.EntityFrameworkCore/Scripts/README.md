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

## Embedded Resource Configuration

These SQL files are configured as **Embedded Resources** in the project file:

```xml
<ItemGroup>
  <EmbeddedResource Include="Scripts\get_formversion_data.sql" />
  <EmbeddedResource Include="Scripts\generate_formversion_view.sql" />
</ItemGroup>
```

## Migration Dependencies

### ⚠️ **CRITICAL MIGRATION NOTE**

The SQL scripts in this folder are deployed via Entity Framework migration:
- **Migration**: `20251010193253_AddFormVersionViewGen.cs`
- **Resource Names**: 
  - `Unity.GrantManager.Scripts.get_formversion_data.sql`
  - `Unity.GrantManager.Scripts.generate_formversion_view.sql`

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
   
   // Repeat for generate_formversion_view.sql
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
- **Data Transformation**: Converting JSON form data into structured relational views
- **Performance**: Pre-computed views for faster reporting queries

## Dependencies

- **PostgreSQL 12+**: Uses advanced JSONB operations and dynamic SQL
- **Entity Framework Core**: Deployed via migrations
- **ABP Framework**: Used within the reporting module context