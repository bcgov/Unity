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

The DbMigrator enables EF Core command logging by default so the migration plan,
SQL command execution, and final migration state are visible in container logs.
Sensitive data logging is not supported by the DbMigrator.

Once you've configured your connection strings via `appsettings.secrets.json` (or environment variables), you can run the migrator:

```bash
dotnet run
```

Migration history flattening is disabled by default. To reconcile databases that still
contain migration ids from the removed migration set, run the migrator once with
`Database__FlattenMigrations=true` (or set `Database:FlattenMigrations` to `true` in
`appsettings.secrets.json`). Do not leave this enabled for normal migration runs: it
deletes migration history rows that were added after the flattened `Initial` migration.

Or run it from Visual Studio by setting `Unity.GrantManager.DbMigrator` as the startup project.

### Status Workflow role defaults

Running DbMigrator first applies host migration `20261126213019_CorrectStatusWorkflowRoleDefaults`, then seeds the host and each tenant's current role defaults. The migration is in `Migrations/HostMigrations` and targets `GrantManagerDbContext`, which owns the shared `PermissionGrants` table.

The migration deletes only Assessor's retired `Start Initial Review` and `Complete Initial Review` role assignments across host and tenant rows. Those two assignments are removed even if manually granted, because the permission table does not distinguish their origin. The SQL is safe when either or both rows are already absent, including a local database that has already applied the correction. Approval/decline, defer after approval, bulk approval, unrelated permissions, and direct user assignments are preserved.

EF migration history records the one-time correction. The permission seeder passes the full current default lists to the insert-only seeder on every run, adding missing grants for both new and existing roles. A default removed by an administrator is restored on the next seed, including approval-related defaults. Additional permissions granted by administrators are preserved, including deliberately restoring an Assessor's initial-review access after the migration. No workflow seed-tracking metadata is read or written, and no migration inserts are needed for new defaults.

`Down()` intentionally leaves permission assignments unchanged: automatically granting both review permissions on rollback would introduce access that some tenants had already removed before the migration. Before squashing migration history, ensure every existing host database has applied this correction or provide an equivalent upgrade path.

Complete DbMigrator before starting the new web application. When `Redis:IsEnabled` is enabled, a background startup worker deletes permission-grant cache keys for the host and each tenant through the existing Redis configuration. The keys come from `StatusWorkflowRoleDefaults`, shared with seeding, plus the two retired Assessor review pairs. This clears cached denials for defaults that seeding restores. Subsequent checks reload values from the database. The deletion runs on every startup and preserves direct user entries, custom-role entries, and unrelated cache data.

The web application handles this cache-key deletion automatically; DbMigrator does not need Redis configuration for this update. Cleanup failures log a warning and retry every 30 seconds in a fresh service scope until successful or the host stops. Connection timeouts and retry delays run in the background, allowing the web application to start. Cached permissions may remain stale until an attempt succeeds. Host shutdown cancels cleanup and retry delays.
