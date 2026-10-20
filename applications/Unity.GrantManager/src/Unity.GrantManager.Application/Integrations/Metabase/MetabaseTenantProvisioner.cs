using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Unity.GrantManager.Identity;
using Unity.GrantManager.Integrations.Exceptions;
using Unity.GrantManager.Permissions;
using Unity.Reporting.Settings;
using Unity.TenantManagement.Metabase;
using Volo.Abp;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.MultiTenancy;
using Volo.Abp.PermissionManagement;
using Volo.Abp.Security.Encryption;
using Volo.Abp.SettingManagement;
using Volo.Abp.Settings;
using Volo.Abp.TenantManagement;
using Volo.Abp.Timing;

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
/// 2. Finds or creates a Metabase permissions group named after the tenant and syncs its members
///    (see <see cref="SyncMembersAsync"/>): adds the program managers, Additional members and
///    holders of Metabase.Access, and removes off-boarded users. A user who isn't already a Metabase user (via LDAP login or
///    Admin &gt; People) is skipped with a warning, not a hard failure.
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
    IRepository<PermissionGrant, Guid> permissionGrantRepository,
    IStringEncryptionService stringEncryptionService,
    ISettingManager settingManager,
    ICurrentTenant currentTenant,
    IDataFilter dataFilter,
    IClock clock,
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

        await SyncMembersAsync(tenantId, groupId);

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

    /// <summary>The tenant's stored Metabase group ID; null when the tenant is gone or not set up.</summary>
    public virtual async Task<int?> FindGroupIdAsync(Guid tenantId)
    {
        using (currentTenant.Change(null))
        {
            var tenant = await tenantRepository.FindAsync(tenantId);
            return tenant?.GetProperty<int?>(GroupIdProperty);
        }
    }

    /// <summary>
    /// Brings the tenant's Metabase group in line with Unity, and records the outcome (or the
    /// failure) as the tenant's last members sync:
    /// - removes members whose email belongs to an off-boarded Unity user in the tenant, whatever
    ///   added them, including people added directly in Metabase;
    /// - removes members this sync added only for Metabase.Access (tracked in
    ///   <see cref="MetabaseSettings.PermissionSyncedEmails"/>) who no longer hold it and aren't
    ///   wanted for another reason;
    /// - adds the program managers, Additional members and permission holders who are missing,
    ///   tracking those added only for the permission.
    /// Everyone else is left alone - in particular a program manager whose role was taken away but
    /// who is still active keeps access, and someone already in the group is never tracked, so this
    /// never removes a member it didn't add. Emails without a Metabase account are skipped until
    /// they sign in.
    /// </summary>
    public virtual async Task<MetabaseMemberSyncResultDto> SyncMembersAsync(Guid tenantId, int groupId)
    {
        try
        {
            var result = await ReconcileMembersAsync(tenantId, groupId);
            await SaveLastMemberSyncAsync(tenantId, new MetabaseLastMemberSyncDto
            {
                SyncedAt = clock.Now,
                Added = result.Added,
                Removed = result.Removed,
                NotMetabaseUsers = result.NotMetabaseUsers
            });
            return result;
        }
        catch (Exception ex) when (ex is IntegrationServiceException or HttpRequestException or TaskCanceledException)
        {
            await SaveLastMemberSyncAsync(tenantId, new MetabaseLastMemberSyncDto { SyncedAt = clock.Now, Error = ex.Message });
            throw;
        }
    }

    private async Task<MetabaseMemberSyncResultDto> ReconcileMembersAsync(Guid tenantId, int groupId)
    {
        var sources = await LoadMemberSourcesAsync(tenantId);
        var tracked = ToEmailSet(SplitEmails(await GetTenantSettingAsync(MetabaseSettings.PermissionSyncedEmails, tenantId)));
        var groupMembers = await metabaseApiClient.GetGroupMembersAsync(groupId);
        var result = new MetabaseMemberSyncResultDto();

        var removed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var member in groupMembers.Where(m =>
            sources.Offboarded.Contains(m.Email) || (tracked.Contains(m.Email) && !sources.IsDesired(m.Email))))
        {
            await metabaseApiClient.RemoveGroupMembershipAsync(member.MembershipId);
            removed.Add(member.Email);
            result.Removed++;
        }

        var inGroup = ToEmailSet(groupMembers.Select(m => m.Email).Where(e => !removed.Contains(e)));
        var addedForPermission = new List<string>();
        foreach (var email in sources.DesiredEmails.Where(e => !inGroup.Contains(e)))
        {
            var userId = await metabaseApiClient.FindUserIdByEmailAsync(email);
            if (userId == null)
            {
                result.NotMetabaseUsers++;
                continue;
            }
            await metabaseApiClient.AddGroupMemberAsync(groupId, userId.Value);
            result.Added++;
            if (sources.IsWantedOnlyForPermission(email))
            {
                addedForPermission.Add(email);
            }
        }

        // Still tracked: added for the permission, still holding it and still in the group. Anyone
        // else drops off, so a later re-add by someone else isn't mistaken for the sync's own.
        var nowTracked = ToEmailSet(tracked
            .Where(e => sources.PermissionHolders.Contains(e) && inGroup.Contains(e))
            .Concat(addedForPermission));
        if (!nowTracked.SetEquals(tracked))
        {
            await SetTenantSettingAsync(MetabaseSettings.PermissionSyncedEmails, string.Join(",", nowTracked), tenantId);
        }

        // Counts, not who: email addresses (even hashed) don't belong in logs.
        logger.LogInformation(
            "{Prefix} Members synced for tenant {TenantId}: {Added} added, {Removed} removed, {NotFound} not yet Metabase users.",
            LogPrefix, tenantId, result.Added, result.Removed, result.NotMetabaseUsers);

        return result;
    }

    /// <summary>The tenant's last members sync; null when none has been recorded.</summary>
    public virtual async Task<MetabaseLastMemberSyncDto?> GetLastMemberSyncAsync(Guid tenantId)
    {
        var value = await GetTenantSettingAsync(MetabaseSettings.LastMemberSync, tenantId);
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<MetabaseLastMemberSyncDto>(value);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private Task SaveLastMemberSyncAsync(Guid tenantId, MetabaseLastMemberSyncDto lastSync) =>
        SetTenantSettingAsync(MetabaseSettings.LastMemberSync, JsonSerializer.Serialize(lastSync), tenantId);

    /// <summary>
    /// Every email in the tenant's Metabase group or wanted there, with why it is listed and its
    /// state. Program managers come first, then permission holders, then Additional members, then
    /// other group members.
    /// </summary>
    public virtual async Task<List<MetabaseMemberDto>> GetMembersAsync(Guid tenantId, int groupId)
    {
        var sources = await LoadMemberSourcesAsync(tenantId);
        var groupMembers = (await metabaseApiClient.GetGroupMembersAsync(groupId))
            .GroupBy(m => m.Email, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var rows = new List<MetabaseMemberDto>();
        foreach (var email in sources.ProgramManagers
            .Concat(sources.PermissionHolders)
            .Concat(sources.AdditionalMembers)
            .Concat(groupMembers.Keys)
            .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            groupMembers.TryGetValue(email, out var member);
            var row = new MetabaseMemberDto
            {
                Email = member?.Email ?? email,
                Name = member?.Name,
                Source = sources.SourceOf(email),
                InGroup = member != null,
                Offboarded = sources.Offboarded.Contains(email),
                CanRemove = !sources.IsManaged(email)
            };

            // Only an email that the next sync would add needs the (one call per email) account lookup.
            if (!row.InGroup && sources.IsDesired(email))
            {
                row.IsMetabaseUser = await metabaseApiClient.FindUserIdByEmailAsync(email) != null;
            }
            rows.Add(row);
        }
        return rows;
    }

    /// <summary>
    /// Adds the emails to the tenant's Additional members list, then syncs, which adds them to the
    /// group (those with a Metabase account) unless they belong to off-boarded users.
    /// </summary>
    public virtual async Task<MetabaseMemberSyncResultDto> AddAdditionalMembersAsync(Guid tenantId, int groupId, IEnumerable<string> emails)
    {
        var additionalMembers = await GetAdditionalMembersAsync(tenantId);
        await SetAdditionalMembersAsync(tenantId, additionalMembers.Concat(emails));
        return await SyncMembersAsync(tenantId, groupId);
    }

    /// <summary>
    /// Takes the email off the Additional members list and out of the group. An active program
    /// manager or permission holder can't be removed here: the sync would add them straight back,
    /// so their role or permission decides.
    /// </summary>
    public virtual async Task RemoveMemberAsync(Guid tenantId, int groupId, string email)
    {
        var sources = await LoadMemberSourcesAsync(tenantId);
        if (sources.IsActiveProgramManager(email))
        {
            throw new UserFriendlyException(
                "This member is a program manager in the tenant. Remove the Program Manager role from the user to remove their Metabase access.");
        }

        if (sources.PermissionHolders.Contains(email))
        {
            throw new UserFriendlyException(
                "This member has the Metabase access permission in the tenant. Remove the permission from the user or their role to remove their Metabase access.");
        }

        if (sources.AdditionalMembers.Contains(email))
        {
            await SetAdditionalMembersAsync(tenantId,
                sources.AdditionalMembers.Where(e => !string.Equals(e, email, StringComparison.OrdinalIgnoreCase)));
        }

        await RemoveFromGroupAsync(groupId, email);
    }

    /// <summary>
    /// Removes the email from the group unless Unity still wants it there. Used when a user is
    /// off-boarded or their email changes, so access goes without waiting for the next sync.
    /// Returns whether a membership was removed.
    /// </summary>
    public virtual async Task<bool> RemoveMemberIfNotDesiredAsync(Guid tenantId, int groupId, string email)
    {
        var sources = await LoadMemberSourcesAsync(tenantId);
        return !sources.IsDesired(email) && await RemoveFromGroupAsync(groupId, email);
    }

    private async Task<bool> RemoveFromGroupAsync(int groupId, string email)
    {
        var member = (await metabaseApiClient.GetGroupMembersAsync(groupId))
            .FirstOrDefault(m => string.Equals(m.Email, email, StringComparison.OrdinalIgnoreCase));
        if (member == null)
        {
            return false;
        }

        await metabaseApiClient.RemoveGroupMembershipAsync(member.MembershipId);
        return true;
    }

    /// <summary>
    /// Unity's view of the tenant's members: program managers (all, active or not), the Additional
    /// members list, active holders of Metabase.Access, and off-boarded emails - those whose every
    /// user in the tenant is deleted or inactive. An email that also belongs to an active user is
    /// not off-boarded, which covers a user deleted and imported again.
    /// </summary>
    private async Task<MemberSources> LoadMemberSourcesAsync(Guid tenantId)
    {
        List<IdentityUser> programManagers;
        List<IdentityUser> users;
        var roleHolders = new List<IdentityUser>();
        List<PermissionGrant> grants;
        using (currentTenant.Change(tenantId))
        {
            programManagers = await identityUserRepository.GetListByNormalizedRoleNameAsync(
                UnityRoles.ProgramManager.ToUpperInvariant());

            // The grant records, not a permission check per user: role and user grants are the only
            // ways the tenant's Roles and Users screens grant it.
            grants = await permissionGrantRepository.GetListAsync(g =>
                g.Name == MetabasePermissions.Access &&
                (g.ProviderName == RolePermissionValueProvider.ProviderName || g.ProviderName == UserPermissionValueProvider.ProviderName));

            foreach (var roleGrant in grants.Where(g => g.ProviderName == RolePermissionValueProvider.ProviderName))
            {
                roleHolders.AddRange(await identityUserRepository.GetListByNormalizedRoleNameAsync(
                    roleGrant.ProviderKey.ToUpperInvariant()));
            }

            using (dataFilter.Disable<ISoftDelete>())
            {
                users = await identityUserRepository.GetListAsync(maxResultCount: int.MaxValue);
            }
        }

        var activeEmails = ToEmailSet(users.Where(u => u.IsActive && !u.IsDeleted).Select(u => u.Email));
        var offboarded = ToEmailSet(users.Where(u => !u.IsActive || u.IsDeleted).Select(u => u.Email));
        offboarded.ExceptWith(activeEmails);

        var userGrantIds = grants
            .Where(g => g.ProviderName == UserPermissionValueProvider.ProviderName)
            .Select(g => Guid.TryParse(g.ProviderKey, out var id) ? id : Guid.Empty)
            .ToHashSet();
        var permissionHolders = ToEmailSet(roleHolders
            .Concat(users.Where(u => userGrantIds.Contains(u.Id)))
            .Where(u => u.IsActive && !u.IsDeleted)
            .Select(u => u.Email));
        permissionHolders.ExceptWith(offboarded);

        return new MemberSources(
            ToEmailSet(programManagers.Select(u => u.Email)),
            ToEmailSet(await GetAdditionalMembersAsync(tenantId)),
            permissionHolders,
            offboarded);
    }

    private async Task<List<string>> GetAdditionalMembersAsync(Guid tenantId) =>
        SplitEmails(await GetTenantSettingAsync(MetabaseSettings.UserEmails, tenantId));

    private Task SetAdditionalMembersAsync(Guid tenantId, IEnumerable<string> emails) =>
        SetTenantSettingAsync(MetabaseSettings.UserEmails, string.Join(",", ToEmailSet(emails)), tenantId);

    private async Task<string?> GetTenantSettingAsync(string name, Guid tenantId)
    {
        using (currentTenant.Change(null))
        {
            return await settingManager.GetOrNullAsync(name, TenantSettingValueProvider.ProviderName, tenantId.ToString());
        }
    }

    private async Task SetTenantSettingAsync(string name, string value, Guid tenantId)
    {
        using (currentTenant.Change(null))
        {
            await settingManager.SetAsync(name, value, TenantSettingValueProvider.ProviderName, tenantId.ToString());
        }
    }

    /// <summary>Splits an email list on commas, semicolons or new lines.</summary>
    public static List<string> SplitEmails(string? emails) =>
        (emails ?? string.Empty)
            .Split([',', ';', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

    private static OrderedEmailSet ToEmailSet(IEnumerable<string?> emails) =>
        new(emails.Where(e => !string.IsNullOrWhiteSpace(e)).Select(e => e!.Trim()));

    /// <summary>Case-insensitive email set that keeps first-seen order, so lists keep their entry order.</summary>
    private sealed class OrderedEmailSet : IEnumerable<string>
    {
        private readonly List<string> _ordered = [];
        private readonly HashSet<string> _set = new(StringComparer.OrdinalIgnoreCase);

        public OrderedEmailSet(IEnumerable<string> emails)
        {
            foreach (var email in emails)
            {
                if (_set.Add(email))
                {
                    _ordered.Add(email);
                }
            }
        }

        public bool Contains(string email) => _set.Contains(email);

        public bool SetEquals(OrderedEmailSet other) => _set.SetEquals(other._set);

        public void ExceptWith(OrderedEmailSet other)
        {
            _set.ExceptWith(other._set);
            _ordered.RemoveAll(e => !_set.Contains(e));
        }

        public IEnumerator<string> GetEnumerator() => _ordered.GetEnumerator();

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }

    /// <summary>
    /// Unity's member sources for one tenant. <see cref="PermissionHolders"/> are already active
    /// and never off-boarded.
    /// </summary>
    private sealed record MemberSources(
        OrderedEmailSet ProgramManagers, OrderedEmailSet AdditionalMembers, OrderedEmailSet PermissionHolders, OrderedEmailSet Offboarded)
    {
        public bool IsDesired(string email) =>
            !Offboarded.Contains(email) &&
            (ProgramManagers.Contains(email) || AdditionalMembers.Contains(email) || PermissionHolders.Contains(email));

        public bool IsActiveProgramManager(string email) => ProgramManagers.Contains(email) && !Offboarded.Contains(email);

        /// <summary>Access follows the role or the permission, so Remove on the members table doesn't apply.</summary>
        public bool IsManaged(string email) => IsActiveProgramManager(email) || PermissionHolders.Contains(email);

        /// <summary>Wanted in the group only because of Metabase.Access, so the sync tracks adding them.</summary>
        public bool IsWantedOnlyForPermission(string email) =>
            PermissionHolders.Contains(email) && !ProgramManagers.Contains(email) && !AdditionalMembers.Contains(email);

        public IEnumerable<string> DesiredEmails =>
            ProgramManagers.Concat(AdditionalMembers).Concat(PermissionHolders)
                .Distinct(StringComparer.OrdinalIgnoreCase).Where(IsDesired);

        public MetabaseMemberSource SourceOf(string email)
        {
            if (ProgramManagers.Contains(email))
            {
                return MetabaseMemberSource.ProgramManager;
            }
            if (PermissionHolders.Contains(email))
            {
                return MetabaseMemberSource.Permission;
            }
            return AdditionalMembers.Contains(email) ? MetabaseMemberSource.AdditionalMember : MetabaseMemberSource.Metabase;
        }
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
