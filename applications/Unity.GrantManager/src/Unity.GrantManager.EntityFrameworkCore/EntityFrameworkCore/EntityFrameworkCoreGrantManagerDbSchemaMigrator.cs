using System;
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
            var connectionString = tenant.ConnectionStrings[0];
            if (connectionString != null)
            {
                var configuration = _serviceProvider.GetRequiredService<IConfiguration>();
                var adminConnectionString = configuration.GetConnectionString(GrantManagerConsts.DefaultConnectionStringName)
                    ?? throw new InvalidOperationException($"Connection string '{GrantManagerConsts.DefaultConnectionStringName}' is not configured.");

                // Decrypt the stored value — plain-text rows (pre-encryption) fall back to their original value.
                // A successful decrypt that produces non-connection-string output (wrong passphrase returning garbage)
                // is also rejected by the Contains('=') check so we surface a meaningful error rather than a
                // cryptic NpgsqlConnectionStringBuilder format exception.
                var rawValue = connectionString.Value;
                string plainValue;
                try
                {
                    var decrypted = _encryptionService.Decrypt(rawValue);
                    plainValue = (decrypted != null && decrypted.Contains('=')) ? decrypted : rawValue;
                }
                catch
                {
                    plainValue = rawValue;
                }

                // Parse the stored tenant connection string to get the DB name and role credentials
                var tenantCsb = new NpgsqlConnectionStringBuilder(plainValue);
                var dbName = tenantCsb.Database
                    ?? throw new InvalidOperationException("Tenant connection string is missing the Database value.");
                var roleName = tenantCsb.Username
                    ?? throw new InvalidOperationException("Tenant connection string is missing the Username value.");
                var rolePassword = tenantCsb.Password
                    ?? throw new InvalidOperationException("Tenant connection string is missing the Password value.");

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

                // Run migrations as admin against the tenant database
                await tenantDb.MigrateAsync();

                // Grant table and sequence privileges after migrations have created all objects
                await GrantTablePrivilegesAsync(adminTenantConnectionString, roleName);

                /* The payments module is also migrated.
                   Currently the payments module also references the tenant connection string.
                   Changes to that may require inspecting the connection string here to resolve
                   the correct one. */
            }
        }
        else
        {
            await _serviceProvider
                .GetRequiredService<GrantManagerDbContext>()
                .Database
                .MigrateAsync();
        }
    }

    private static async Task CreateRoleIfNotExistsAsync(string adminConnectionString, string roleName, string password)
    {
        await using var conn = new NpgsqlConnection(adminConnectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $"""
            DO $$
            BEGIN
              IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = '{roleName}') THEN
                CREATE ROLE "{roleName}" WITH LOGIN PASSWORD '{password}';
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
}
