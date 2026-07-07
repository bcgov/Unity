# Unity.GrantManager.DbMigrator

This project is responsible for running database migrations for the Unity Grant Manager application.

## Local Development Setup

The `appsettings.json` file in this project intentionally excludes sensitive information like database passwords to comply with security best practices and avoid SonarQube or Copilot warnings about checking credentials into source control.

### appsettings.secrets.json (used by this project)

`Program.cs` wires configuration up with:

```csharp
Host.CreateDefaultBuilder(args)
    .AddAppSettingsSecretsJson()
    ...
```

`AddAppSettingsSecretsJson()` is a helper from the ABP Framework (`Volo.Abp.Core`) that adds an **optional** `appsettings.secrets.json` file to the configuration pipeline. Because it's chained onto the host builder after `Host.CreateDefaultBuilder` has already added its default sources, it loads *after* those sources (including environment variables and command-line arguments).
As a result, any value it defines will override values from earlier providers (including env vars and command-line) when the keys match.

Create an `appsettings.secrets.json` file in this project directory with your local connection strings and any other overrides:

```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;port=5432;Database=UnityGrantManager;Username=postgres;Password=your_password_here",
    "Tenant": "Host=localhost;port=5432;Database=UnityGrantTenant;Username=postgres;Password=your_password_here",
    "Onboarding": "Host=localhost;port=5432;Database=UnityOnboarding;Username=postgres;Password=your_password_here"
  }
}
```

**Note:** This file is excluded from git via the root [`.gitignore`](../../../../.gitignore) (`/applications/Unity.GrantManager/src/Unity.GrantManager.DbMigrator/appsettings.secrets.json`), so your local credentials are never committed. The `Unity.GrantManager.Web` project uses the same mechanism and has its own gitignore entry for its own `appsettings.secrets.json`.

Because the file is optional, its absence is not an error — omit it entirely if you're configuring everything through environment variables instead.

### Production / CI builds

`appsettings.secrets.json` is **not** referenced anywhere in `Unity.GrantManager.DbMigrator.csproj` (unlike `appsettings.json`, which is explicitly included with `CopyToOutputDirectory`), so even if the file exists on a machine building this project, it is not copied into the build or publish output. Combined with the file being gitignored — meaning a fresh CI checkout never has it on disk in the first place — there's no path for local secrets to leak into a DbMigrator build or the resulting Docker image.

## Running the Migrator

Once you've configured your connection strings via `appsettings.secrets.json` (or environment variables), you can run the migrator:

```bash
dotnet run
```

Or run it from Visual Studio by setting `Unity.GrantManager.DbMigrator` as the startup project.
