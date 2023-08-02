# Unity Grant Manager Application

## Getting Started

1. With .NET Core installed, run `dotnet tool install -g Volo.Abp.Cli`
2. **Load JS Dependencies**: Navigate to `.\applications\Unity.GrantManager` and run `abp install-libs` to install JavaScript dependencies.
3. **Create the Database**: If you are using Visual Studio, right click on the `Unity.GrantManager.DbMigrator` project, select Set as StartUp Project, then hit Ctrl+F5 to run it without debugging. It will create the initial database and seed the initial data.
4. **Run the Web Application**: Right click on the `Unity.GrantManager.Web` project, select Set as StartUp Project and run the application (F5 or Ctrl+F5 in Visual Studio).

### Creating Entity Framework Migrations

1. Update the entity classes and DbContext class
2. In `Unity.GrantManager.EntityFrameworkCore` run `dotnet ef migrations add <Name of your migration script>` in your command line.
    - To remove a newly created migration script, run `dotnet ef migrations remove`.
3. Run the migration scripts by setting `Unity.GrantManager.DbMigrator` and running it.
    - Right click on `Unity.GrantManager.DbMigrator` and click **"Set As Startup Project"**
    - `Ctrl + F5` to run without debugging

## See Also

- https://docs.abp.io/en/abp/latest
- https://docs.abp.io/en/abp/latest/Domain-Driven-Design
- https://github.com/abpframework/abp
- https://github.com/abpframework/abp-samples

- [ABP Platform Roadmap](https://docs.abp.io/en/abp/latest/Road-Map)
- [ABP Platform Module Packages](https://abp.io/packages)

- [EF Core Database Migrations](https://docs.abp.io/en/abp/latest/Entity-Framework-Core-Migrations) 
