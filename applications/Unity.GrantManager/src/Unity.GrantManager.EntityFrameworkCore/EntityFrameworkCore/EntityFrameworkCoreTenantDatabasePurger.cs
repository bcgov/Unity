using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Security.Encryption;
using Volo.Abp.TenantManagement;

namespace Unity.GrantManager.EntityFrameworkCore;

public class EntityFrameworkCoreTenantDatabasePurger(
    IConfiguration configuration,
    IStringEncryptionService encryptionService,
    ILogger<EntityFrameworkCoreTenantDatabasePurger> logger)
    : ITenantDatabasePurger, ITransientDependency
{
    /* Host tables holding a tenant's own data, deleted by TenantId. Child tables (UserRoles,
     * UserClaims, RoleClaims, OrganizationUnitRoles, ...) cascade from Users/Roles/OrganizationUnits,
     * and TenantConnectionStrings cascades from Tenants. Audit tables (AuditLogs, EntityChanges,
     * SecurityLogs, ExceptionLogs) are deliberately kept as history.
     */
    private static readonly string[] TenantIdTables =
    [
        "Users",
        "Roles",
        "OrganizationUnits",
        "Sessions",
        "UserDelegations",
        "PermissionGrants",
        "ResourcePermissionGrants",
        "DynamicUrls",
        "TenantTokens",
        "ApplicantTenantMaps"
    ];

    // Feature and setting values are stored against the tenant by provider ("T" = tenant).
    private static readonly string[] TenantProviderTables = ["FeatureValues", "Settings"];

    public async Task PurgeAsync(Tenant tenant, string licencePlate)
    {
        var adminConnectionString = configuration.GetConnectionString(GrantManagerConsts.DefaultConnectionStringName)
            ?? throw new InvalidOperationException($"Connection string '{GrantManagerConsts.DefaultConnectionStringName}' is not configured.");
        var hostDatabaseName = new NpgsqlConnectionStringBuilder(adminConnectionString).Database;

        var tenantCsb = ReadConnectionString(tenant, GrantManagerConsts.DefaultTenantConnectionStringName)
            ?? throw new InvalidOperationException($"Tenant '{tenant.Name}' has no '{GrantManagerConsts.DefaultTenantConnectionStringName}' connection string.");
        var dbName = tenantCsb.Database
            ?? throw new InvalidOperationException("Tenant connection string is missing the Database value.");

        // Only ever drop the tenant's own generated database - never a configured/shared one.
        if (!string.Equals(dbName, licencePlate, StringComparison.Ordinal)
            || string.Equals(dbName, hostDatabaseName, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Tenant '{tenant.Name}' connection string points at database '{dbName}', not its licence plate '{licencePlate}'. Refusing to drop it.");
        }

        // Role names come from the immutable licence plate (TenantConnectionStringBuilder creates
        // them as {plate} and {plate}_readonly), never from the editable connection strings - an
        // edited Username must not make purge drop an unrelated login.
        var roleNames = new List<string>
        {
            licencePlate,
            $"{licencePlate}_readonly"
        };

        EntityFrameworkCoreGrantManagerDbSchemaMigrator.EnsureSafeIdentifier(dbName, "database name");
        foreach (var roleName in roleNames)
        {
            EntityFrameworkCoreGrantManagerDbSchemaMigrator.EnsureSafeIdentifier(roleName, "role name");
        }

        LogSucceededPostCreationSteps(tenant);

        // Database and roles first: if anything below fails, the tenant row is still there
        // (soft-deleted) and the purge can simply be run again - every step is idempotent.
        await DropDatabaseAndRolesAsync(adminConnectionString, dbName, roleNames);
        await DeleteHostRecordsAsync(adminConnectionString, tenant.Id);

        logger.LogInformation("Purged tenant {TenantName} ({TenantId}): dropped database {DatabaseName} and roles {RoleNames}.",
            tenant.Name, tenant.Id, dbName, string.Join(", ", roleNames));
    }

    private NpgsqlConnectionStringBuilder? ReadConnectionString(Tenant tenant, string name)
    {
        var raw = tenant.FindConnectionString(name);
        if (raw == null)
        {
            return null;
        }

        // Same fallback as the schema migrator: plain-text rows (pre-encryption) are used as-is.
        string value;
        try
        {
            var decrypted = encryptionService.Decrypt(raw);
            value = decrypted != null && decrypted.Contains('=') ? decrypted : raw;
        }
        catch
        {
            value = raw;
        }

        return new NpgsqlConnectionStringBuilder(value);
    }

    // Metabase resources are not removed here: their IDs aren't stored and dev/dev2 share one
    // Metabase, so a name-based delete could hit another environment. Leave a trail instead.
    private void LogSucceededPostCreationSteps(Tenant tenant)
    {
        var succeeded = tenant.ExtraProperties
            .Where(p => p.Key.StartsWith("PostCreationStep_", StringComparison.Ordinal)
                && p.Key.EndsWith("_Status", StringComparison.Ordinal)
                && string.Equals(p.Value?.ToString(), "Success", StringComparison.OrdinalIgnoreCase))
            .Select(p => p.Key)
            .ToList();

        if (succeeded.Count > 0)
        {
            logger.LogWarning(
                "Purging tenant {TenantName} ({TenantId}): post-creation steps {Steps} had succeeded; their external resources (e.g. Metabase) are not removed and need manual cleanup.",
                tenant.Name, tenant.Id, string.Join(", ", succeeded));
        }
    }

    private static async Task DropDatabaseAndRolesAsync(string adminConnectionString, string dbName, List<string> roleNames)
    {
        await using var conn = new NpgsqlConnection(adminConnectionString);
        await conn.OpenAsync();

        // WITH (FORCE) terminates open connections (e.g. pooled web connections) - Postgres 13+.
        await using (var dropDb = conn.CreateCommand())
        {
            dropDb.CommandText = $"DROP DATABASE IF EXISTS \"{dbName}\" WITH (FORCE)";
            await dropDb.ExecuteNonQueryAsync();
        }

        // The roles only hold grants inside the dropped database (migrations run as admin, so
        // they own nothing), which is why DROP ROLE succeeds once the database is gone.
        foreach (var roleName in roleNames)
        {
            await using var dropRole = conn.CreateCommand();
            dropRole.CommandText = $"DROP ROLE IF EXISTS \"{roleName}\"";
            await dropRole.ExecuteNonQueryAsync();
        }
    }

    private static async Task DeleteHostRecordsAsync(string adminConnectionString, Guid tenantId)
    {
        await using var conn = new NpgsqlConnection(adminConnectionString);
        await conn.OpenAsync();
        await using var transaction = await conn.BeginTransactionAsync();

        foreach (var table in TenantIdTables)
        {
            await ExecuteAsync(conn, transaction, $"DELETE FROM \"{table}\" WHERE \"TenantId\" = @tenantId", tenantId);
        }

        foreach (var table in TenantProviderTables)
        {
            await ExecuteAsync(conn, transaction,
                $"DELETE FROM \"{table}\" WHERE \"ProviderName\" = 'T' AND \"ProviderKey\" = @tenantId::text", tenantId);
        }

        await ExecuteAsync(conn, transaction, "DELETE FROM \"Tenants\" WHERE \"Id\" = @tenantId", tenantId);

        await transaction.CommitAsync();
    }

    private static async Task ExecuteAsync(NpgsqlConnection conn, NpgsqlTransaction transaction, string sql, Guid tenantId)
    {
        await using var cmd = new NpgsqlCommand(sql, conn, transaction);
        cmd.Parameters.AddWithValue("tenantId", tenantId);
        await cmd.ExecuteNonQueryAsync();
    }
}
