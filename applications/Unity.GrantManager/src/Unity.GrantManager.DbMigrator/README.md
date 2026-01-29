# Unity.GrantManager.DbMigrator

This project is responsible for running database migrations for the Unity Grant Manager application.

## Local Development Setup

The `appsettings.json` file in this project intentionally excludes sensitive information like database passwords to comply with security best practices and avoid SonarQube or Copilot warnings about checking credentials into source control.

### Option 1: Using appsettings.Development.json (Recommended for Local Development)

Create an `appsettings.Development.json` file in the DbMigrator project directory with your connection strings including passwords:

```json
{
  "ConnectionStrings": {    
    "Default": "Host=localhost;port=5432;Database=UnityGrantManager;Username=postgres;Password=your_password_here",
    "Tenant": "Host=localhost;port=5432;Database=UnityGrantTenant;Username=postgres;Password=your_password_here"
  }
}
```

**Note:** This file is excluded from git via `.gitignore` to keep your credentials secure.

### Option 2: Using User Secrets

Alternatively, you can use .NET User Secrets to store sensitive configuration:

1. Right-click on the `Unity.GrantManager.DbMigrator` project in Visual Studio
2. Select "Manage User Secrets"
3. Add your connection strings with passwords:

```json
{
  "ConnectionStrings": {    
    "Default": "Host=localhost;port=5432;Database=UnityGrantManager;Username=postgres;Password=your_password_here",
    "Tenant": "Host=localhost;port=5432;Database=UnityGrantTenant;Username=postgres;Password=your_password_here"
  }
}
```

User secrets are stored outside of your project directory and are never checked into source control.

## Running the Migrator

Once you've configured your connection strings using either option above, you can run the migrator:

```bash
dotnet run
```

Or run it from Visual Studio by setting `Unity.GrantManager.DbMigrator` as the startup project.
