using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Unity.GrantManager.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Security.Encryption;
using Volo.Abp.TenantManagement;

namespace Unity.GrantManager.EntityFrameworkCore;

public class EntityFrameworkCoreGrantManagerDbSchemaMigrator
    : IGrantManagerDbSchemaMigrator, ITransientDependency
{
    /* Migration history was squashed to a single "Initial" migration per context
     * (see Migrations/HostMigrations and Migrations/TenantMigrations). Databases that
     * were already migrated under the old, now-deleted migration set still carry
     * __EFMigrationsHistory rows for those old migration ids, which don't match
     * "Initial" and would make Database.MigrateAsync() below try to re-run Initial's
     * CreateTable operations against a schema that already has them.
     *
     * ReconcileMigrationHistoryAsync resets history to just the Initial row *before*
     * MigrateAsync() is called, so EF sees it as already applied and skips it. Brand
     * new databases (including newly provisioned tenants) have an empty or nonexistent
     * history table at this point, so the reconciliation is a no-op and MigrateAsync()
     * runs Initial for real to build the schema. Safe to run unconditionally on every
     * migrator invocation, forever - after the first run per database, history only
     * ever contains the Initial row so the guard clause never fires again.
     */
    private const string HostInitialMigrationId = "20260722193713_Initial";
    private const string TenantInitialMigrationId = "20260721203242_Initial";
    private const string EfCoreProductVersion = "10.0.3";

    private readonly IServiceProvider _serviceProvider;
    private readonly IStringEncryptionService _encryptionService;

    public EntityFrameworkCoreGrantManagerDbSchemaMigrator(
        IServiceProvider serviceProvider,
        IStringEncryptionService encryptionService)
    {
        _serviceProvider = serviceProvider;
        _encryptionService = encryptionService;
    }

    public async Task MigrateAsync(Tenant? tenant)
    {
        /* We intentionally resolve GrantManagerDbContext / GrantTenantDbContext
         * from IServiceProvider (instead of directly injecting) to properly get
         * the connection string of the current tenant in the current scope.
         */

        if (tenant != null)
        {
            var connectionString = tenant.FindConnectionString(GrantManagerConsts.DefaultTenantConnectionStringName);
            if (connectionString != null)
            {
                var configuration = _serviceProvider.GetRequiredService<IConfiguration>();
                var adminConnectionString = configuration.GetConnectionString(GrantManagerConsts.DefaultConnectionStringName)
                    ?? throw new InvalidOperationException($"Connection string '{GrantManagerConsts.DefaultConnectionStringName}' is not configured.");

                // Parse the stored tenant connection string to get the DB name and role credentials
                var tenantCsb = new NpgsqlConnectionStringBuilder(TryDecryptConnectionString(connectionString));
                var dbName = tenantCsb.Database
                    ?? throw new InvalidOperationException("Tenant connection string is missing the Database value.");
                var roleName = tenantCsb.Username
                    ?? throw new InvalidOperationException("Tenant connection string is missing the Username value.");
                var rolePassword = tenantCsb.Password
                    ?? throw new InvalidOperationException("Tenant connection string is missing the Password value.");

                // dbName/roleName get interpolated directly into admin DDL below (Postgres
                // identifiers can't be parameterized) — enforce an allowlist so a connection
                // string crafted via UpdateConnectionStringsAsync can't break out of an
                // identifier and inject arbitrary SQL into this admin-privileged migration path.
                EnsureSafeIdentifier(dbName, "database name");
                EnsureSafeIdentifier(roleName, "role name");

                // Build an admin-level connection string targeting the tenant database
                var adminCsb = new NpgsqlConnectionStringBuilder(adminConnectionString) { Database = dbName };
                var adminTenantConnectionString = adminCsb.ToString();

                // Create the PostgreSQL role (idempotent via DO block)
                await CreateRoleIfNotExistsAsync(adminConnectionString, roleName, rolePassword);

                // Create the database if it does not exist; use the admin connection so
                // MigrateAsync connects cleanly and avoids logging a ConnectionError.
                var tenantDb = _serviceProvider
                    .GetRequiredService<GrantTenantDbContext>()
                    .Database;

                tenantDb.SetConnectionString(adminTenantConnectionString);

                if (!await tenantDb.CanConnectAsync())
                {
                    await tenantDb.GetService<IRelationalDatabaseCreator>().CreateAsync();
                }

                // Grant database and schema privileges to the role (idempotent)
                await GrantDatabasePrivilegesAsync(adminConnectionString, dbName, roleName);
                await GrantSchemaPrivilegesAsync(adminTenantConnectionString, roleName);

                // Ensure __EFMigrationsHistory exists so MigrateAsync does not log a CommandError
                await tenantDb.ExecuteSqlRawAsync(
                    tenantDb.GetService<IHistoryRepository>().GetCreateIfNotExistsScript());

                await ReconcileMigrationHistoryAsync(tenantDb, TenantInitialMigrationId);

                // Run migrations as admin against the tenant database
                await tenantDb.MigrateAsync();

                // Grant table and sequence privileges after migrations have created all objects
                await GrantTablePrivilegesAsync(adminTenantConnectionString, roleName);

                // Provision the readonly role/connection (if a readonly connection string was generated for this tenant)
                var readOnlyConnectionString = tenant.FindConnectionString(GrantManagerConsts.DefaultTenantReadOnlyConnectionStringName);
                if (readOnlyConnectionString != null)
                {
                    var readOnlyCsb = new NpgsqlConnectionStringBuilder(TryDecryptConnectionString(readOnlyConnectionString));
                    var readOnlyRoleName = readOnlyCsb.Username
                        ?? throw new InvalidOperationException("Tenant readonly connection string is missing the Username value.");
                    var readOnlyPassword = readOnlyCsb.Password
                        ?? throw new InvalidOperationException("Tenant readonly connection string is missing the Password value.");
                    EnsureSafeIdentifier(readOnlyRoleName, "readonly role name");

                    await CreateRoleIfNotExistsAsync(adminConnectionString, readOnlyRoleName, readOnlyPassword);
                    await GrantReadOnlyPrivilegesAsync(adminConnectionString, adminTenantConnectionString, dbName, readOnlyRoleName);
                }

                /* The payments module is also migrated.
                   Currently the payments module also references the tenant connection string.
                   Changes to that may require inspecting the connection string here to resolve
                   the correct one. */
            }
        }
        else
        {
            var hostDb = _serviceProvider
                .GetRequiredService<GrantManagerDbContext>()
                .Database;

            // The database itself may not exist yet on a brand new Postgres instance.
            // MigrateAsync() would normally create it as its first step, but
            // ReconcileMigrationHistoryAsync needs to connect before that, so ensure
            // it exists here first (mirrors the tenant path further up).
            if (!await hostDb.CanConnectAsync())
            {
                await hostDb.GetService<IRelationalDatabaseCreator>().CreateAsync();
            }

            await ReconcileMigrationHistoryAsync(hostDb, HostInitialMigrationId);

            await hostDb.MigrateAsync();
        }
    }

    private static async Task ReconcileMigrationHistoryAsync(DatabaseFacade database, string initialMigrationId)
    {
        // initialMigrationId/EfCoreProductVersion are hardcoded constants above, never
        // external or tenant-controlled input, so interpolating them here is safe.
#pragma warning disable EF1002
        await database.ExecuteSqlRawAsync($"""
            DO $$
            BEGIN
                IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = '__EFMigrationsHistory') THEN
                    IF EXISTS (SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" <> '{initialMigrationId}') THEN
                        DELETE FROM public."__EFMigrationsHistory";
                        INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
                        VALUES ('{initialMigrationId}', '{EfCoreProductVersion}');
                    END IF;
                END IF;
            END $$;
            """);
#pragma warning restore EF1002
    }

    // Decrypt the stored value — plain-text rows (pre-encryption) fall back to their original value.
    // A successful decrypt that produces non-connection-string output (wrong passphrase returning garbage)
    // is also rejected by the Contains('=') check so we surface a meaningful error rather than a
    // cryptic NpgsqlConnectionStringBuilder format exception.
    private string TryDecryptConnectionString(string rawValue)
    {
        try
        {
            var decrypted = _encryptionService.Decrypt(rawValue);
            return (decrypted != null && decrypted.Contains('=')) ? decrypted : rawValue;
        }
        catch
        {
            return rawValue;
        }
    }

    // Postgres identifiers (role/database/schema names) can't be parameterized via the wire
    // protocol, so every call site that interpolates one into DDL must first validate it
    // against this allowlist rather than relying on escaping alone.
    private static readonly Regex SafeIdentifierPattern = new("^[A-Za-z0-9_-]+$", RegexOptions.Compiled);

    private static void EnsureSafeIdentifier(string value, string fieldName)
    {
        if (!SafeIdentifierPattern.IsMatch(value))
        {
            throw new InvalidOperationException(
                $"Tenant connection string {fieldName} '{value}' contains characters that are not allowed in a PostgreSQL identifier.");
        }
    }

    // Passwords are used as quoted string literals (not identifiers), so unlike role/database
    // names they keep a richer allowed character set — escaped here instead of allowlisted.
    private static string EscapeSqlLiteral(string value) => value.Replace("'", "''");

    private static async Task CreateRoleIfNotExistsAsync(string adminConnectionString, string roleName, string password)
    {
        await using var conn = new NpgsqlConnection(adminConnectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $"""
            DO $$
            BEGIN
              IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = '{roleName}') THEN
                CREATE ROLE "{roleName}" WITH LOGIN PASSWORD '{EscapeSqlLiteral(password)}';
              END IF;
            END
            $$;
            """;
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task GrantDatabasePrivilegesAsync(string adminConnectionString, string dbName, string roleName)
    {
        await using var conn = new NpgsqlConnection(adminConnectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $"GRANT ALL PRIVILEGES ON DATABASE \"{dbName}\" TO \"{roleName}\"";
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task GrantSchemaPrivilegesAsync(string adminTenantConnectionString, string roleName)
    {
        await using var conn = new NpgsqlConnection(adminTenantConnectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $"GRANT ALL PRIVILEGES ON SCHEMA public TO \"{roleName}\"";
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task GrantTablePrivilegesAsync(string adminTenantConnectionString, string roleName)
    {
        await using var conn = new NpgsqlConnection(adminTenantConnectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();

        // Iterate over every non-system schema so schemas like "Notifications" created
        // by migrations are covered in addition to the default "public" schema.
        cmd.CommandText = $"""
            DO $$
            DECLARE
                r RECORD;
            BEGIN
                FOR r IN
                    SELECT nspname FROM pg_namespace
                    WHERE nspname NOT IN ('pg_catalog', 'information_schema', 'pg_toast')
                      AND nspname NOT LIKE 'pg_%'
                LOOP
                    EXECUTE format('GRANT ALL PRIVILEGES ON SCHEMA %I TO "{roleName}"', r.nspname);
                    EXECUTE format('GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA %I TO "{roleName}"', r.nspname);
                    EXECUTE format('GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA %I TO "{roleName}"', r.nspname);
                    EXECUTE format('ALTER DEFAULT PRIVILEGES IN SCHEMA %I GRANT ALL ON TABLES TO "{roleName}"', r.nspname);
                    EXECUTE format('ALTER DEFAULT PRIVILEGES IN SCHEMA %I GRANT ALL ON SEQUENCES TO "{roleName}"', r.nspname);
                END LOOP;
            END
            $$;
            """;
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task GrantReadOnlyPrivilegesAsync(string adminConnectionString, string adminTenantConnectionString, string dbName, string roleName)
    {
        await using (var conn = new NpgsqlConnection(adminConnectionString))
        {
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = $"GRANT CONNECT ON DATABASE \"{dbName}\" TO \"{roleName}\"";
            await cmd.ExecuteNonQueryAsync();
        }

        await using (var conn = new NpgsqlConnection(adminTenantConnectionString))
        {
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();

            // Grant read-only access (USAGE/SELECT) on every non-system schema, including
            // tables and sequences created by future migrations via ALTER DEFAULT PRIVILEGES.
            cmd.CommandText = $"""
                DO $$
                DECLARE
                    r RECORD;
                BEGIN
                    FOR r IN
                        SELECT nspname FROM pg_namespace
                        WHERE nspname NOT IN ('pg_catalog', 'information_schema', 'pg_toast')
                          AND nspname NOT LIKE 'pg_%'
                    LOOP
                        EXECUTE format('GRANT USAGE ON SCHEMA %I TO "{roleName}"', r.nspname);
                        EXECUTE format('GRANT SELECT ON ALL TABLES IN SCHEMA %I TO "{roleName}"', r.nspname);
                        EXECUTE format('GRANT SELECT ON ALL SEQUENCES IN SCHEMA %I TO "{roleName}"', r.nspname);
                        EXECUTE format('ALTER DEFAULT PRIVILEGES IN SCHEMA %I GRANT SELECT ON TABLES TO "{roleName}"', r.nspname);
                        EXECUTE format('ALTER DEFAULT PRIVILEGES IN SCHEMA %I GRANT SELECT ON SEQUENCES TO "{roleName}"', r.nspname);
                    END LOOP;
                END
                $$;
                """;
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
