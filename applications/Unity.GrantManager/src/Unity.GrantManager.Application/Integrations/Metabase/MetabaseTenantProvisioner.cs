using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Unity.GrantManager.Identity;
using Unity.GrantManager.Integrations.Exceptions;
using Unity.Reporting.Settings;
using Unity.TenantManagement.Metabase;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Identity;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Security.Encryption;
using Volo.Abp.SettingManagement;
using Volo.Abp.Settings;
using Volo.Abp.TenantManagement;

namespace Unity.GrantManager.Integrations.Metabase;

/// <summary>
/// Registers a tenant in Metabase. Does everything manual_deploy_new_metabase_tenant.ps1 used to
/// do by hand, minus the OpenShift/psql steps (the tenant's readonly Postgres role + credentials
/// already exist by this point, since EntityFrameworkCoreGrantManagerDbSchemaMigrator provisions
/// them automatically at tenant creation):
///
/// 1. Connects the tenant's data as a read-only source - decrypts the tenant's stored
///    Tenant_Readonly connection string, saves its username as the tenant's reporting view role
///    (<see cref="ReportingSettings.TenantViewRole"/>), and calls the Metabase API to find or create
///    a database connection named after the tenant, then triggers a schema sync + value rescan.
/// 2. Finds or creates a Metabase permissions group named after the tenant and adds the tenant's
///    members to it (see <see cref="GetMemberEmailsAsync"/>). A user who isn't already a Metabase
///    user (via LDAP login or Admin &gt; People) is skipped with a warning, not a hard failure.
/// 3. Grants that group unrestricted view/query access to the new database connection, scoped to
///    just this tenant's data.
/// 4. Finds or creates a Metabase collection for the tenant and grants the group write access to it.
/// 5. Stores the database, group and collection IDs in the tenant's ExtraProperties. On a tenant
///    registered before the IDs were stored, the find-or-create calls return the existing
///    resources, so running Setup again backfills the IDs without creating anything.
///
/// Once IDs are stored, a rerun updates those resources by ID and renames them to the tenant's
/// current name, so renaming a tenant and running Resync renames its Metabase resources instead
/// of creating a second set. Find-or-create by name is only the fallback for a missing ID.
///
/// Every Metabase call (via <see cref="IMetabaseApiClient"/>) is idempotent by design
/// (update by stored ID or find-or-create by tenant name, membership/permission checks before
/// writing), so a rerun is always safe.
/// </summary>
public class MetabaseTenantProvisioner(
    IMetabaseApiClient metabaseApiClient,
    ITenantRepository tenantRepository,
    IIdentityUserRepository identityUserRepository,
    IStringEncryptionService stringEncryptionService,
    ISettingManager settingManager,
    ICurrentTenant currentTenant,
    IOptions<MetabaseOptions> metabaseOptions,
    ILogger<MetabaseTenantProvisioner> logger)
    : ITransientDependency
{
    // Tenant ExtraProperties keys, stored as plain scalars like the other Tenant entries.
    public const string DatabaseIdProperty = "MetabaseDatabaseId";
    public const string GroupIdProperty = "MetabaseGroupId";
    public const string CollectionIdProperty = "MetabaseCollectionId";

    private const string LogPrefix = "[Metabase]";
    private const string TenantReadOnlyConnectionStringName = "Tenant_Readonly";

    public virtual async Task SetupAsync(Guid tenantId)
    {
        // Tenant is host-side data, whichever context the caller is in.
        Tenant tenant;
        using (currentTenant.Change(null))
        {
            tenant = await tenantRepository.GetAsync(tenantId, includeDetails: true);
        }

        var encryptedReadOnlyConnectionString = tenant.FindConnectionString(TenantReadOnlyConnectionStringName);
        // Throwing (not returning) so the caller records an error: without this connection
        // string nothing can be provisioned.
        if (string.IsNullOrWhiteSpace(encryptedReadOnlyConnectionString))
        {
            throw new InvalidOperationException(
                $"Tenant '{tenant.Name}' has no {TenantReadOnlyConnectionStringName} connection string, so it cannot be registered in Metabase.");
        }

        var (host, port, dbName, username, password) =
            ParseConnectionString(stringEncryptionService.Decrypt(encryptedReadOnlyConnectionString));

        if (!string.IsNullOrWhiteSpace(metabaseOptions.Value.DbHostOverride))
        {
            logger.LogInformation(
                "{Prefix} Overriding Postgres host '{OriginalHost}' with '{OverrideHost}' for tenant {TenantId} (Metabase:DbHostOverride is set).",
                LogPrefix, host, metabaseOptions.Value.DbHostOverride, tenantId);
            host = metabaseOptions.Value.DbHostOverride;
        }

        // Metabase reads as this role, so it's the one the generated reporting views must be
        // granted to (TenantViewRoleManager reads this setting, falling back to {LicencePlate}_readonly
        // when it's unset).
        using (currentTenant.Change(null))
        {
            await settingManager.SetAsync(
                ReportingSettings.TenantViewRole, username, TenantSettingValueProvider.ProviderName, tenantId.ToString());
        }

        var ssl = metabaseOptions.Value.DbSslOverride ?? true;

        // Stored IDs win over the name lookup: after a tenant rename, finding by the new name would
        // create a second database, group and collection and orphan the first set.
        var databaseId = await ReuseOrFindOrCreateAsync(
            tenant.GetProperty<int?>(DatabaseIdProperty),
            id => metabaseApiClient.UpdateDatabaseDetailsAsync(id, tenant.Name, host, port, dbName, username, password, ssl),
            () => metabaseApiClient.FindOrCreateDatabaseAsync(tenant.Name, host, port, dbName, username, password, ssl),
            "database", tenantId);
        await metabaseApiClient.SyncDatabaseSchemaAsync(databaseId);
        await metabaseApiClient.RescanDatabaseValuesAsync(databaseId);

        var groupId = await ReuseOrFindOrCreateAsync(
            tenant.GetProperty<int?>(GroupIdProperty),
            id => metabaseApiClient.RenameGroupAsync(id, tenant.Name),
            () => metabaseApiClient.FindOrCreateGroupAsync(tenant.Name),
            "group", tenantId);

        var notFoundCount = 0;
        foreach (var email in await GetMemberEmailsAsync(tenantId))
        {
            var userId = await metabaseApiClient.FindUserIdByEmailAsync(email);
            if (userId == null)
            {
                notFoundCount++;
                continue;
            }
            await metabaseApiClient.AddGroupMemberAsync(groupId, userId.Value);
        }

        // A count, not who: email addresses (even hashed) don't belong in logs.
        if (notFoundCount > 0)
        {
            logger.LogWarning(
                "{Prefix} {NotFoundCount} member(s) not found in Metabase for tenant {TenantId} - they must log in via LDAP or be created under Admin > People before they can be added to the group.",
                LogPrefix, notFoundCount, tenantId);
        }

        await metabaseApiClient.GrantGroupDatabaseAccessAsync(groupId, databaseId);

        var collectionId = await ReuseOrFindOrCreateAsync(
            tenant.GetProperty<int?>(CollectionIdProperty),
            id => metabaseApiClient.RenameCollectionAsync(id, tenant.Name),
            () => metabaseApiClient.FindOrCreateCollectionAsync(tenant.Name),
            "collection", tenantId);
        await metabaseApiClient.GrantGroupCollectionAccessAsync(groupId, collectionId);

        // Reload rather than reuse the tenant from the start: the Metabase calls take minutes, and
        // a save in between (e.g. the Configuration modal) would fail this update's concurrency check.
        using (currentTenant.Change(null))
        {
            var current = await tenantRepository.GetAsync(tenantId);
            current.SetProperty(DatabaseIdProperty, databaseId);
            current.SetProperty(GroupIdProperty, groupId);
            current.SetProperty(CollectionIdProperty, collectionId);
            await tenantRepository.UpdateAsync(current);
        }

        logger.LogInformation(
            "{Prefix} Registration complete for tenant {TenantId} ('{TenantName}'): database={DatabaseId}, group={GroupId}, collection={CollectionId}",
            LogPrefix, tenantId, tenant.Name, databaseId, groupId, collectionId);
    }

    /// <summary>
    /// Updates (and renames to the tenant's current name) the resource the tenant already stores
    /// an ID for; finds or creates one by name only when there's no stored ID, or Metabase no
    /// longer has it (deleted by hand).
    /// </summary>
    private async Task<int> ReuseOrFindOrCreateAsync(
        int? storedId, Func<int, Task> update, Func<Task<int>> findOrCreate, string resource, Guid tenantId)
    {
        if (storedId.HasValue)
        {
            try
            {
                await update(storedId.Value);
                return storedId.Value;
            }
            catch (IntegrationServiceException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                logger.LogWarning(
                    "{Prefix} Stored Metabase {Resource} #{Id} for tenant {TenantId} no longer exists - finding or creating one by name.",
                    LogPrefix, resource, storedId.Value, tenantId);
            }
        }

        return await findOrCreate();
    }

    /// <summary>
    /// The tenant's active program managers (users with the program_manager role) plus its
    /// Additional members list (the tenant-scoped <see cref="MetabaseSettings.UserEmails"/>
    /// setting), de-duplicated case-insensitively. There is no Global default to fall back to.
    /// </summary>
    private async Task<List<string>> GetMemberEmailsAsync(Guid tenantId)
    {
        List<IdentityUser> programManagers;
        using (currentTenant.Change(tenantId))
        {
            programManagers = await identityUserRepository.GetListByNormalizedRoleNameAsync(
                UnityRoles.ProgramManager.ToUpperInvariant());
        }

        var additionalMembers = await settingManager.GetOrNullAsync(
            MetabaseSettings.UserEmails, TenantSettingValueProvider.ProviderName, tenantId.ToString());

        return programManagers
            .Where(u => u.IsActive && !string.IsNullOrWhiteSpace(u.Email))
            .Select(u => u.Email.Trim())
            .Concat((additionalMembers ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static (string Host, int Port, string DbName, string Username, string Password) ParseConnectionString(string connectionString)
    {
        string? Get(string key)
        {
            foreach (var part in connectionString.Split(';'))
            {
                var eq = part.IndexOf('=');
                if (eq > 0 && string.Equals(part[..eq].Trim(), key, StringComparison.OrdinalIgnoreCase))
                {
                    return part[(eq + 1)..].Trim();
                }
            }
            return null;
        }

        var host = Get("Host") ?? throw new InvalidOperationException("Tenant readonly connection string is missing Host.");
        var dbName = Get("Database") ?? throw new InvalidOperationException("Tenant readonly connection string is missing Database.");
        var username = Get("Username") ?? throw new InvalidOperationException("Tenant readonly connection string is missing Username.");
        var password = Get("Password") ?? throw new InvalidOperationException("Tenant readonly connection string is missing Password.");
        var port = int.Parse(Get("Port") ?? "5432", CultureInfo.InvariantCulture);

        return (host, port, dbName, username, password);
    }
}
