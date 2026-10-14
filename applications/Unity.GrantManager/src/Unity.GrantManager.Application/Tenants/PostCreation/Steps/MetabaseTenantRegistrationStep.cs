using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Unity.GrantManager.Integrations;
using Unity.GrantManager.Integrations.Metabase;
using Unity.Modules.Shared.Features;
using Unity.Modules.Shared.PostTenantCreation;
using Unity.TenantManagement.Metabase;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Features;
using Volo.Abp.Security.Encryption;
using Volo.Abp.Settings;
using Volo.Abp.SettingManagement;
using Volo.Abp.TenantManagement;

namespace Unity.GrantManager.Tenants.PostCreation.Steps;

/// <summary>
/// Runs as post-tenant-creation step <c>Order = 1</c> and does everything
/// manual_deploy_new_metabase_tenant.ps1 used to do by hand, minus the OpenShift/psql steps (the
/// tenant's readonly Postgres role + credentials already exist by this point, since
/// EntityFrameworkCoreGrantManagerDbSchemaMigrator provisions them automatically at tenant creation):
///
/// 1. Connects the tenant's data as a read-only source - decrypts the tenant's stored
///    Tenant_Readonly connection string and calls the Metabase API to find or create a database
///    connection named after the tenant, then triggers a schema sync + value rescan.
/// 2. Finds or creates a Metabase permissions group named after the tenant and adds the
///    configured member emails to it (see <see cref="GetUserEmailsAsync"/>). A user who isn't
///    already a Metabase user (via LDAP login or Admin &gt; People) is skipped with a warning, not
///    a hard failure.
/// 3. Grants that group unrestricted view/query access to the new database connection, scoped to
///    just this tenant's data.
/// 4. Finds or creates a Metabase collection for the tenant and grants the group write access to it.
///
/// The member email list is the tenant-scoped <see cref="MetabaseSettings.UserEmails"/> setting;
/// there is no Global default to fall back to.
///
/// <see cref="CanExecuteAsync"/> records the step as Skipped when Metabase is disabled for the
/// environment or the tenant lacks the Unity.Reporting feature, and as Failure when a Reporting
/// tenant needs Metabase but its API key or METABASE_API_BASE endpoint is missing.
///
/// <see cref="ContinueOnError"/> is true - a Metabase outage is logged but doesn't block tenant
/// creation or later post-creation steps. Recovery from a partial failure is a manual re-enqueue
/// of this step (see <c>PostTenantCreationSequenceJob</c>) - every Metabase call this step makes
/// (via <see cref="IMetabaseApiClient"/>) is idempotent by design (find-or-create by tenant name,
/// membership/permission checks before writing) so a rerun is always safe.
/// </summary>
[RemoteService(false)]
[ExposeServices(typeof(IPostTenantCreationStep))]
public class MetabaseTenantRegistrationStep(
    IMetabaseApiClient metabaseApiClient,
    ITenantRepository tenantRepository,
    IStringEncryptionService stringEncryptionService,
    ISettingManager settingManager,
    IOptions<MetabaseOptions> metabaseOptions,
    IEndpointManagementAppService endpointManagementAppService,
    IFeatureChecker featureChecker,
    ILogger<MetabaseTenantRegistrationStep> logger)
    : IPostTenantCreationStep, ITransientDependency
{
    private const string LogPrefix = "[PostTenantCreation][Metabase]";
    private const string TenantReadOnlyConnectionStringName = "Tenant_Readonly";

    public int Order => 1;

    public string Key => "MetabaseSync";

    public string StepName => "Metabase Tenant Registration";

    // A Metabase outage shouldn't block other post-creation steps from running.
    public bool ContinueOnError => true;

    // Runs inside the tenant's context (PostTenantCreationSequenceJob), so the feature check
    // reads this tenant's Unity.Reporting value.
    public virtual async Task<PostTenantCreationStepCheck> CanExecuteAsync(Guid tenantId)
    {
        if (!metabaseOptions.Value.Enabled)
        {
            return PostTenantCreationStepCheck.Skip("Metabase is not enabled for this environment.");
        }

        // Before the config checks: a tenant without Reporting never uses Metabase, so a missing
        // key or URL isn't a failure for it.
        if (!await featureChecker.IsEnabledAsync(FeatureConsts.Reporting))
        {
            return PostTenantCreationStepCheck.Skip($"The tenant does not have the {FeatureConsts.Reporting} feature.");
        }

        if (string.IsNullOrWhiteSpace(metabaseOptions.Value.ApiKey))
        {
            return PostTenantCreationStepCheck.Fail("Metabase is enabled but no API key is configured.");
        }

        if (!await IsMetabaseUrlConfiguredAsync())
        {
            return PostTenantCreationStepCheck.Fail(
                $"Metabase is enabled but the {DynamicUrlKeyNames.METABASE_API_BASE} endpoint is not set.");
        }

        return PostTenantCreationStepCheck.Run;
    }

    private async Task<bool> IsMetabaseUrlConfiguredAsync()
    {
        try
        {
            // Throws when the endpoint row is missing or its URL is blank.
            await endpointManagementAppService.GetUgmUrlByKeyNameAsync(DynamicUrlKeyNames.METABASE_API_BASE);
            return true;
        }
        catch (UserFriendlyException)
        {
            return false;
        }
    }

    public virtual async Task ExecuteAsync(Guid tenantId)
    {
        var tenant = await tenantRepository.GetAsync(tenantId, includeDetails: true);

        var encryptedReadOnlyConnectionString = tenant.FindConnectionString(TenantReadOnlyConnectionStringName);
        if (string.IsNullOrWhiteSpace(encryptedReadOnlyConnectionString))
        {
            logger.LogWarning(
                "{Prefix} No readonly connection string found for tenant {TenantId} ('{TenantName}') - skipping.",
                LogPrefix, tenantId, tenant.Name);
            return;
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

        var ssl = metabaseOptions.Value.DbSslOverride ?? true;

        var databaseId = await metabaseApiClient.FindOrCreateDatabaseAsync(tenant.Name, host, port, dbName, username, password, ssl);
        await metabaseApiClient.SyncDatabaseSchemaAsync(databaseId);
        await metabaseApiClient.RescanDatabaseValuesAsync(databaseId);

        var groupId = await metabaseApiClient.FindOrCreateGroupAsync(tenant.Name);

        foreach (var email in await GetUserEmailsAsync(tenantId))
        {
            var userId = await metabaseApiClient.FindUserIdByEmailAsync(email);
            if (userId == null)
            {
                logger.LogWarning(
                    "{Prefix} User (hash {EmailHash}) not found in Metabase for tenant {TenantId} - they must log in via LDAP or be created under Admin > People before they can be added to a group.",
                    LogPrefix, HashEmail(email), tenantId);
                continue;
            }
            await metabaseApiClient.AddGroupMemberAsync(groupId, userId.Value);
        }

        await metabaseApiClient.GrantGroupDatabaseAccessAsync(groupId, databaseId);

        var collectionId = await metabaseApiClient.FindOrCreateCollectionAsync(tenant.Name);
        await metabaseApiClient.GrantGroupCollectionAccessAsync(groupId, collectionId);

        logger.LogInformation(
            "{Prefix} Registration complete for tenant {TenantId} ('{TenantName}'): database={DatabaseId}, group={GroupId}, collection={CollectionId}",
            LogPrefix, tenantId, tenant.Name, databaseId, groupId, collectionId);
    }

    private async Task<List<string>> GetUserEmailsAsync(Guid tenantId)
    {
        var raw = await settingManager.GetOrNullAsync(MetabaseSettings.UserEmails, TenantSettingValueProvider.ProviderName, tenantId.ToString());

        return (raw ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    // Avoids writing a user's email address to logs (CodeQL: exposure of private information).
    // A substring/mask still contains real characters from the source string, so CodeQL's
    // dataflow analysis still treats it as the same private data - a one-way hash is what's
    // actually recognized as breaking that taint, while still letting an admin correlate repeated
    // "not found" warnings against a known list of emails (by hashing candidates themselves).
    private static string HashEmail(string email) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(email.Trim().ToLowerInvariant())))[..8];

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
