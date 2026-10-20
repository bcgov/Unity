using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using Unity.GrantManager.Identity;
using Unity.GrantManager.Integrations.Exceptions;
using Unity.GrantManager.Permissions;
using Unity.Reporting.Settings;
using Unity.TenantManagement.Metabase;
using Volo.Abp;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.MultiTenancy;
using Volo.Abp.PermissionManagement;
using Volo.Abp.Security.Encryption;
using Volo.Abp.SettingManagement;
using Volo.Abp.Settings;
using Volo.Abp.TenantManagement;
using Volo.Abp.Timing;
using Xunit;

namespace Unity.GrantManager.Integrations.Metabase;

public class MetabaseTenantProvisionerTests
{
    private const string ReadOnlyConnectionStringName = "Tenant_Readonly";
    private const string DecryptedConnectionString =
        "Host=dev-crunchy-postgres-primary.ce395f-dev.svc;Port=5432;Database=T_ABC123;Username=t_abc123_readonly;Password=s3cr3t;";

    private sealed record Fixture(
        MetabaseTenantProvisioner Provisioner,
        IMetabaseApiClient MetabaseApiClient,
        ISettingManager SettingManager,
        ITenantRepository TenantRepository,
        List<IdentityUser> ProgramManagers,
        List<IdentityUser> OtherUsers,
        List<MetabaseGroupMember> GroupMembers,
        Tenant Tenant,
        List<PermissionGrant> Grants,
        Dictionary<string, List<IdentityUser>> RoleUsers,
        Dictionary<string, string?> Settings);

    private static readonly DateTime Now = new(2026, 10, 19, 17, 2, 0, DateTimeKind.Utc);

    // Tenant's constructors are all non-public (ABP requires going through ITenantManager to
    // create one) - reflection is the standard workaround for exercising its instance state
    // (SetConnectionString/FindConnectionString) in a plain, DB-less unit test.
    private static Tenant CreateTenant(string name)
    {
        var ctor = typeof(Tenant).GetConstructor(
            BindingFlags.NonPublic | BindingFlags.Instance,
            null, [typeof(Guid), typeof(string), typeof(string)], null)!;
        return (Tenant)ctor.Invoke([Guid.NewGuid(), name, name.ToUpperInvariant()]);
    }

    private static Fixture CreateProvisioner(
        string? encryptedReadOnlyConnectionString = "encrypted-blob", string? dbHostOverride = null, bool? dbSslOverride = null,
        string? additionalMembers = null)
    {
        var tenant = CreateTenant("AG-MARB");
        if (encryptedReadOnlyConnectionString != null)
        {
            tenant.SetConnectionString(ReadOnlyConnectionStringName, encryptedReadOnlyConnectionString);
        }

        var tenantRepository = Substitute.For<ITenantRepository>();
        tenantRepository.GetAsync(tenant.Id, Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(tenant);

        // The role query never returns deleted users; the off-boarding check sees every user,
        // deleted ones included, so program managers are part of that list too.
        var programManagers = new List<IdentityUser>();
        var otherUsers = new List<IdentityUser>();
        var roleUsers = new Dictionary<string, List<IdentityUser>>();
        var identityUserRepository = Substitute.For<IIdentityUserRepository>();
        identityUserRepository.GetListByNormalizedRoleNameAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(ci => roleUsers.TryGetValue(ci.ArgAt<string>(0), out var users)
                ? users.Where(u => !u.IsDeleted).ToList()
                : new List<IdentityUser>());
        identityUserRepository.GetListByNormalizedRoleNameAsync(UnityRoles.ProgramManager.ToUpperInvariant(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(_ => programManagers.Where(u => !u.IsDeleted).ToList());
        identityUserRepository.GetListAsync(maxResultCount: int.MaxValue)
            .ReturnsForAnyArgs(_ => programManagers.Concat(otherUsers).ToList());

        var encryptionService = Substitute.For<IStringEncryptionService>();
        encryptionService.Decrypt(Arg.Any<string>()).Returns(DecryptedConnectionString);

        var settingManager = Substitute.For<ISettingManager>();
        settingManager.GetOrNullAsync(MetabaseSettings.UserEmails, TenantSettingValueProvider.ProviderName, tenant.Id.ToString())
            .Returns(additionalMembers);

        // The tracking list and the last sync round-trip, so a test can run two syncs.
        var settings = new Dictionary<string, string?>();
        foreach (var name in new[] { MetabaseSettings.PermissionSyncedEmails, MetabaseSettings.LastMemberSync })
        {
            settingManager.GetOrNullAsync(name, TenantSettingValueProvider.ProviderName, tenant.Id.ToString())
                .Returns(_ => settings.GetValueOrDefault(name));
            settingManager.When(s => s.SetAsync(name, Arg.Any<string?>(), TenantSettingValueProvider.ProviderName, tenant.Id.ToString(), Arg.Any<bool>()))
                .Do(ci => settings[name] = ci.ArgAt<string?>(1));
        }

        // Applies the provisioner's own filter, so a grant for another permission or provider is ignored.
        var grants = new List<PermissionGrant>();
        var permissionGrantRepository = Substitute.For<IRepository<PermissionGrant, Guid>>();
        permissionGrantRepository.GetListAsync(Arg.Any<Expression<Func<PermissionGrant, bool>>>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(ci => grants.Where(ci.ArgAt<Expression<Func<PermissionGrant, bool>>>(0).Compile()).ToList());

        var clock = Substitute.For<IClock>();
        clock.Now.Returns(Now);

        var currentTenant = Substitute.For<ICurrentTenant>();
        currentTenant.Change(Arg.Any<Guid?>(), Arg.Any<string?>()).Returns(Substitute.For<IDisposable>());

        var metabaseApiClient = Substitute.For<IMetabaseApiClient>();
        metabaseApiClient.FindOrCreateDatabaseAsync(tenant.Name, Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>())
            .Returns(11);
        metabaseApiClient.FindOrCreateGroupAsync(tenant.Name).Returns(22);
        metabaseApiClient.FindOrCreateCollectionAsync(tenant.Name).Returns(33);
        var groupMembers = new List<MetabaseGroupMember>();
        metabaseApiClient.GetGroupMembersAsync(Arg.Any<int>()).Returns(_ => groupMembers);

        var metabaseOptions = Options.Create(new MetabaseOptions
        {
            Enabled = true,
            ApiKey = "test-api-key",
            DbHostOverride = dbHostOverride ?? string.Empty,
            DbSslOverride = dbSslOverride
        });

        var provisioner = new MetabaseTenantProvisioner(
            metabaseApiClient, tenantRepository, identityUserRepository, permissionGrantRepository, encryptionService, settingManager,
            currentTenant, Substitute.For<IDataFilter>(), clock, metabaseOptions, Substitute.For<ILogger<MetabaseTenantProvisioner>>());

        return new Fixture(provisioner, metabaseApiClient, settingManager, tenantRepository, programManagers, otherUsers, groupMembers, tenant,
            grants, roleUsers, settings);
    }

    /// <summary>Gives the role Metabase.Access and puts the user in it.</summary>
    private static IdentityUser AddRoleHolder(Fixture f, string roleName, IdentityUser user)
    {
        if (!f.RoleUsers.ContainsKey(roleName.ToUpperInvariant()))
        {
            f.RoleUsers[roleName.ToUpperInvariant()] = [];
            f.Grants.Add(new PermissionGrant(Guid.NewGuid(), MetabasePermissions.Access, RolePermissionValueProvider.ProviderName, roleName, f.Tenant.Id));
        }
        f.RoleUsers[roleName.ToUpperInvariant()].Add(user);
        f.OtherUsers.Add(user);
        return user;
    }

    /// <summary>Gives the user Metabase.Access directly.</summary>
    private static IdentityUser AddUserHolder(Fixture f, IdentityUser user)
    {
        f.Grants.Add(new PermissionGrant(Guid.NewGuid(), MetabasePermissions.Access, UserPermissionValueProvider.ProviderName, user.Id.ToString(), f.Tenant.Id));
        f.OtherUsers.Add(user);
        return user;
    }

    private static string? Tracked(Fixture f) => f.Settings.GetValueOrDefault(MetabaseSettings.PermissionSyncedEmails);

    private static IdentityUser CreateUser(string email, bool isActive = true, bool isDeleted = false)
    {
        var user = new IdentityUser(Guid.NewGuid(), email, email) { IsDeleted = isDeleted };
        user.SetIsActive(isActive);
        return user;
    }

    private static MetabaseGroupMember GroupMember(string email, int membershipId) =>
        new(membershipId + 1000, membershipId, email, null);

    [Fact]
    public async Task SetupAsync_NoReadonlyConnectionString_ThrowsWithoutCallingMetabase()
    {
        var f = CreateProvisioner(encryptedReadOnlyConnectionString: null);

        var ex = await Should.ThrowAsync<InvalidOperationException>(() => f.Provisioner.SetupAsync(f.Tenant.Id));

        ex.Message.ShouldContain(ReadOnlyConnectionStringName);

        await f.MetabaseApiClient.DidNotReceiveWithAnyArgs().FindOrCreateDatabaseAsync(default!, default!, default, default!, default!, default!, default);
    }

    [Fact]
    public async Task SetupAsync_CreatesDatabaseGroupAndCollection_UsingParsedConnectionDetails()
    {
        var f = CreateProvisioner(additionalMembers: "user1@gov.bc.ca,user2@gov.bc.ca");
        f.MetabaseApiClient.FindUserIdByEmailAsync("user1@gov.bc.ca").Returns(101);
        f.MetabaseApiClient.FindUserIdByEmailAsync("user2@gov.bc.ca").Returns((int?)null);

        await f.Provisioner.SetupAsync(f.Tenant.Id);

        await f.MetabaseApiClient.Received(1).FindOrCreateDatabaseAsync(
            f.Tenant.Name, "dev-crunchy-postgres-primary.ce395f-dev.svc", 5432, "T_ABC123", "t_abc123_readonly", "s3cr3t", true);
        await f.MetabaseApiClient.Received(1).SyncDatabaseSchemaAsync(11);
        await f.MetabaseApiClient.Received(1).RescanDatabaseValuesAsync(11);
        await f.MetabaseApiClient.Received(1).AddGroupMemberAsync(22, 101);
        await f.MetabaseApiClient.DidNotReceive().AddGroupMemberAsync(22, Arg.Is<int>(id => id != 101));
        await f.MetabaseApiClient.Received(1).GrantGroupDatabaseAccessAsync(22, 11);
        await f.MetabaseApiClient.Received(1).GrantGroupCollectionAccessAsync(22, 33);
    }

    [Fact]
    public async Task SetupAsync_SavesReadOnlyUsernameAsTenantViewRole()
    {
        var f = CreateProvisioner();

        await f.Provisioner.SetupAsync(f.Tenant.Id);

        await f.SettingManager.Received(1).SetAsync(
            ReportingSettings.TenantViewRole, "t_abc123_readonly", TenantSettingValueProvider.ProviderName, f.Tenant.Id.ToString(), Arg.Any<bool>());
    }

    [Fact]
    public async Task SetupAsync_StoresDatabaseGroupAndCollectionIdsOnTenant()
    {
        var f = CreateProvisioner();

        await f.Provisioner.SetupAsync(f.Tenant.Id);

        f.Tenant.GetProperty<int>(MetabaseTenantProvisioner.DatabaseIdProperty).ShouldBe(11);
        f.Tenant.GetProperty<int>(MetabaseTenantProvisioner.GroupIdProperty).ShouldBe(22);
        f.Tenant.GetProperty<int>(MetabaseTenantProvisioner.CollectionIdProperty).ShouldBe(33);
        await f.TenantRepository.Received(1).UpdateAsync(f.Tenant, Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    // The Metabase calls take minutes; saving the tenant loaded at the start would fail the
    // concurrency check if anything saved the tenant meanwhile (e.g. the Configuration modal).
    [Fact]
    public async Task SetupAsync_StoresIdsOnTenantReloadedAfterMetabaseCalls()
    {
        var f = CreateProvisioner();
        var reloaded = CreateTenant(f.Tenant.Name);
        f.TenantRepository.GetAsync(f.Tenant.Id, Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(f.Tenant, reloaded);

        await f.Provisioner.SetupAsync(f.Tenant.Id);

        reloaded.GetProperty<int>(MetabaseTenantProvisioner.DatabaseIdProperty).ShouldBe(11);
        await f.TenantRepository.Received(1).UpdateAsync(reloaded, Arg.Any<bool>(), Arg.Any<CancellationToken>());
        await f.TenantRepository.DidNotReceive().UpdateAsync(f.Tenant, Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetupAsync_ExistingTenantWithoutStoredIds_BackfillsIdsOfExistingResources()
    {
        // A tenant registered before IDs were stored: find-or-create returns the existing
        // resources by name, and Setup records their IDs.
        var f = CreateProvisioner();
        f.Tenant.HasProperty(MetabaseTenantProvisioner.DatabaseIdProperty).ShouldBeFalse();
        f.MetabaseApiClient.FindOrCreateDatabaseAsync(f.Tenant.Name, Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>())
            .Returns(5);
        f.MetabaseApiClient.FindOrCreateGroupAsync(f.Tenant.Name).Returns(6);
        f.MetabaseApiClient.FindOrCreateCollectionAsync(f.Tenant.Name).Returns(7);

        await f.Provisioner.SetupAsync(f.Tenant.Id);

        f.Tenant.GetProperty<int>(MetabaseTenantProvisioner.DatabaseIdProperty).ShouldBe(5);
        f.Tenant.GetProperty<int>(MetabaseTenantProvisioner.GroupIdProperty).ShouldBe(6);
        f.Tenant.GetProperty<int>(MetabaseTenantProvisioner.CollectionIdProperty).ShouldBe(7);
    }

    [Fact]
    public async Task SetupAsync_StoredIds_RenamesThemToTenantNameInsteadOfFindingByName()
    {
        // A tenant renamed after Setup: the stored resources are renamed, not duplicated.
        var f = CreateProvisioner();
        f.Tenant.SetProperty(MetabaseTenantProvisioner.DatabaseIdProperty, 46);
        f.Tenant.SetProperty(MetabaseTenantProvisioner.GroupIdProperty, 140);
        f.Tenant.SetProperty(MetabaseTenantProvisioner.CollectionIdProperty, 130);

        await f.Provisioner.SetupAsync(f.Tenant.Id);

        await f.MetabaseApiClient.Received(1).UpdateDatabaseDetailsAsync(
            46, f.Tenant.Name, "dev-crunchy-postgres-primary.ce395f-dev.svc", 5432, "T_ABC123", "t_abc123_readonly", "s3cr3t", true);
        await f.MetabaseApiClient.Received(1).RenameGroupAsync(140, f.Tenant.Name);
        await f.MetabaseApiClient.Received(1).RenameCollectionAsync(130, f.Tenant.Name);
        await f.MetabaseApiClient.DidNotReceiveWithAnyArgs().FindOrCreateDatabaseAsync(default!, default!, default, default!, default!, default!, default);
        await f.MetabaseApiClient.DidNotReceiveWithAnyArgs().FindOrCreateGroupAsync(default!);
        await f.MetabaseApiClient.DidNotReceiveWithAnyArgs().FindOrCreateCollectionAsync(default!);
        await f.MetabaseApiClient.Received(1).GrantGroupDatabaseAccessAsync(140, 46);
        await f.MetabaseApiClient.Received(1).GrantGroupCollectionAccessAsync(140, 130);
        f.Tenant.GetProperty<int>(MetabaseTenantProvisioner.DatabaseIdProperty).ShouldBe(46);
        f.Tenant.GetProperty<int>(MetabaseTenantProvisioner.GroupIdProperty).ShouldBe(140);
        f.Tenant.GetProperty<int>(MetabaseTenantProvisioner.CollectionIdProperty).ShouldBe(130);
    }

    [Fact]
    public async Task SetupAsync_StoredGroupDeletedInMetabase_FallsBackToFindOrCreate()
    {
        var f = CreateProvisioner();
        f.Tenant.SetProperty(MetabaseTenantProvisioner.GroupIdProperty, 140);
        f.MetabaseApiClient.RenameGroupAsync(140, f.Tenant.Name).ThrowsAsync(
            new IntegrationServiceException("Metabase API call to '/api/permissions/group/140' failed with status NotFound") { StatusCode = HttpStatusCode.NotFound });

        await f.Provisioner.SetupAsync(f.Tenant.Id);

        await f.MetabaseApiClient.Received(1).FindOrCreateGroupAsync(f.Tenant.Name);
        f.Tenant.GetProperty<int>(MetabaseTenantProvisioner.GroupIdProperty).ShouldBe(22);
    }

    [Fact]
    public async Task SetupAsync_StoredIdRenameFailsOtherThanNotFound_Throws()
    {
        var f = CreateProvisioner();
        f.Tenant.SetProperty(MetabaseTenantProvisioner.CollectionIdProperty, 130);
        f.MetabaseApiClient.RenameCollectionAsync(130, f.Tenant.Name).ThrowsAsync(
            new IntegrationServiceException("Metabase API call to '/api/collection/130' failed with status Forbidden") { StatusCode = HttpStatusCode.Forbidden });

        await Should.ThrowAsync<IntegrationServiceException>(() => f.Provisioner.SetupAsync(f.Tenant.Id));

        await f.MetabaseApiClient.DidNotReceiveWithAnyArgs().FindOrCreateCollectionAsync(default!);
    }

    [Fact]
    public async Task SetupAsync_DbHostOverrideConfigured_UsesOverrideHostInsteadOfConnectionStringHost()
    {
        var f = CreateProvisioner(dbHostOverride: "host.docker.internal");

        await f.Provisioner.SetupAsync(f.Tenant.Id);

        await f.MetabaseApiClient.Received(1).FindOrCreateDatabaseAsync(
            f.Tenant.Name, "host.docker.internal", 5432, "T_ABC123", "t_abc123_readonly", "s3cr3t", true);
    }

    [Fact]
    public async Task SetupAsync_DbSslOverrideFalse_DisablesSslForDatabaseConnection()
    {
        var f = CreateProvisioner(dbSslOverride: false);

        await f.Provisioner.SetupAsync(f.Tenant.Id);

        await f.MetabaseApiClient.Received(1).FindOrCreateDatabaseAsync(
            f.Tenant.Name, "dev-crunchy-postgres-primary.ce395f-dev.svc", 5432, "T_ABC123", "t_abc123_readonly", "s3cr3t", false);
    }

    [Fact]
    public async Task SetupAsync_AddsProgramManagersAndAdditionalMembers()
    {
        var f = CreateProvisioner(additionalMembers: "extra@gov.bc.ca");
        f.ProgramManagers.Add(CreateUser("pm@gov.bc.ca"));
        f.MetabaseApiClient.FindUserIdByEmailAsync("pm@gov.bc.ca").Returns(201);
        f.MetabaseApiClient.FindUserIdByEmailAsync("extra@gov.bc.ca").Returns(202);

        await f.Provisioner.SetupAsync(f.Tenant.Id);

        await f.MetabaseApiClient.Received(1).AddGroupMemberAsync(22, 201);
        await f.MetabaseApiClient.Received(1).AddGroupMemberAsync(22, 202);
    }

    [Fact]
    public async Task SetupAsync_ProgramManagerAlsoAnAdditionalMember_LooksUpOnce()
    {
        var f = CreateProvisioner(additionalMembers: "PM@gov.bc.ca");
        f.ProgramManagers.Add(CreateUser("pm@gov.bc.ca"));

        await f.Provisioner.SetupAsync(f.Tenant.Id);

        await f.MetabaseApiClient.ReceivedWithAnyArgs(1).FindUserIdByEmailAsync(default!);
    }

    [Fact]
    public async Task SetupAsync_InactiveProgramManager_IsNotAdded()
    {
        var f = CreateProvisioner();
        f.ProgramManagers.Add(CreateUser("gone@gov.bc.ca", isActive: false));

        await f.Provisioner.SetupAsync(f.Tenant.Id);

        await f.MetabaseApiClient.DidNotReceiveWithAnyArgs().FindUserIdByEmailAsync(default!);
    }

    [Fact]
    public async Task SetupAsync_UsesTenantScopedAdditionalMembers_NeverTheGlobalDefault()
    {
        var f = CreateProvisioner(additionalMembers: "tenant-scoped@gov.bc.ca");

        await f.Provisioner.SetupAsync(f.Tenant.Id);

        await f.MetabaseApiClient.Received(1).FindUserIdByEmailAsync("tenant-scoped@gov.bc.ca");
        await f.SettingManager.DidNotReceive().GetOrNullGlobalAsync(MetabaseSettings.UserEmails);
    }

    [Fact]
    public async Task SetupAsync_NoProgramManagersOrAdditionalMembers_AddsNoMembers()
    {
        var f = CreateProvisioner();

        await f.Provisioner.SetupAsync(f.Tenant.Id);

        await f.MetabaseApiClient.DidNotReceiveWithAnyArgs().FindUserIdByEmailAsync(default!);
        await f.SettingManager.DidNotReceive().GetOrNullGlobalAsync(MetabaseSettings.UserEmails);
    }

    [Fact]
    public async Task SyncMembersAsync_OffboardedMembers_RemovedWhateverAddedThem()
    {
        var f = CreateProvisioner();
        f.ProgramManagers.Add(CreateUser("inactive-pm@gov.bc.ca", isActive: false));
        f.OtherUsers.Add(CreateUser("deleted@gov.bc.ca", isDeleted: true));
        f.GroupMembers.Add(GroupMember("inactive-pm@gov.bc.ca", 1));
        f.GroupMembers.Add(GroupMember("DELETED@gov.bc.ca", 2));
        f.GroupMembers.Add(GroupMember("metabase-only@gov.bc.ca", 3));

        var result = await f.Provisioner.SyncMembersAsync(f.Tenant.Id, 22);

        result.Removed.ShouldBe(2);
        await f.MetabaseApiClient.Received(1).RemoveGroupMembershipAsync(1);
        await f.MetabaseApiClient.Received(1).RemoveGroupMembershipAsync(2);
        await f.MetabaseApiClient.DidNotReceive().RemoveGroupMembershipAsync(3);
    }

    [Fact]
    public async Task SyncMembersAsync_DeletedThenReimportedUser_IsNotOffboarded()
    {
        var f = CreateProvisioner();
        f.OtherUsers.Add(CreateUser("back@gov.bc.ca", isDeleted: true));
        f.OtherUsers.Add(CreateUser("back@gov.bc.ca"));
        f.GroupMembers.Add(GroupMember("back@gov.bc.ca", 1));

        var result = await f.Provisioner.SyncMembersAsync(f.Tenant.Id, 22);

        result.Removed.ShouldBe(0);
        await f.MetabaseApiClient.DidNotReceiveWithAnyArgs().RemoveGroupMembershipAsync(default);
    }

    [Fact]
    public async Task SyncMembersAsync_DowngradedProgramManagerStillActive_KeepsAccess()
    {
        var f = CreateProvisioner();
        f.OtherUsers.Add(CreateUser("former-pm@gov.bc.ca"));
        f.GroupMembers.Add(GroupMember("former-pm@gov.bc.ca", 1));

        await f.Provisioner.SyncMembersAsync(f.Tenant.Id, 22);

        await f.MetabaseApiClient.DidNotReceiveWithAnyArgs().RemoveGroupMembershipAsync(default);
    }

    [Fact]
    public async Task SyncMembersAsync_AddsOnlyMissingMembers_AndCountsThoseWithoutAMetabaseAccount()
    {
        var f = CreateProvisioner(additionalMembers: "already@gov.bc.ca,new@gov.bc.ca,not-yet@gov.bc.ca");
        f.GroupMembers.Add(GroupMember("Already@gov.bc.ca", 1));
        f.MetabaseApiClient.FindUserIdByEmailAsync("new@gov.bc.ca").Returns(301);
        f.MetabaseApiClient.FindUserIdByEmailAsync("not-yet@gov.bc.ca").Returns((int?)null);

        var result = await f.Provisioner.SyncMembersAsync(f.Tenant.Id, 22);

        result.Added.ShouldBe(1);
        result.NotMetabaseUsers.ShouldBe(1);
        await f.MetabaseApiClient.DidNotReceive().FindUserIdByEmailAsync("already@gov.bc.ca");
        await f.MetabaseApiClient.Received(1).AddGroupMemberAsync(22, 301);
    }

    [Fact]
    public async Task SyncMembersAsync_OffboardedAdditionalMember_SkippedButKeptOnList()
    {
        var f = CreateProvisioner(additionalMembers: "gone@gov.bc.ca");
        f.OtherUsers.Add(CreateUser("gone@gov.bc.ca", isDeleted: true));

        await f.Provisioner.SyncMembersAsync(f.Tenant.Id, 22);

        await f.MetabaseApiClient.DidNotReceiveWithAnyArgs().FindUserIdByEmailAsync(default!);
        await f.SettingManager.DidNotReceive().SetAsync(
            MetabaseSettings.UserEmails, Arg.Any<string?>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<bool>());
    }

    [Fact]
    public async Task SetupAsync_RemovesOffboardedMembers()
    {
        var f = CreateProvisioner();
        f.OtherUsers.Add(CreateUser("deleted@gov.bc.ca", isDeleted: true));
        f.GroupMembers.Add(GroupMember("deleted@gov.bc.ca", 7));

        await f.Provisioner.SetupAsync(f.Tenant.Id);

        await f.MetabaseApiClient.Received(1).RemoveGroupMembershipAsync(7);
    }

    [Fact]
    public async Task GetMembersAsync_ListsEverySourceWithStateAndRemovability()
    {
        var f = CreateProvisioner(additionalMembers: "extra@gov.bc.ca,gone@gov.bc.ca");
        f.ProgramManagers.Add(CreateUser("pm@gov.bc.ca"));
        f.OtherUsers.Add(CreateUser("gone@gov.bc.ca", isDeleted: true));
        f.GroupMembers.Add(GroupMember("pm@gov.bc.ca", 1));
        f.GroupMembers.Add(GroupMember("analyst@gov.bc.ca", 2));
        f.MetabaseApiClient.FindUserIdByEmailAsync("extra@gov.bc.ca").Returns((int?)null);

        var rows = await f.Provisioner.GetMembersAsync(f.Tenant.Id, 22);

        rows.Select(r => r.Email).ShouldBe(["pm@gov.bc.ca", "extra@gov.bc.ca", "gone@gov.bc.ca", "analyst@gov.bc.ca"]);

        var pm = rows[0];
        pm.Source.ShouldBe(MetabaseMemberSource.ProgramManager);
        pm.InGroup.ShouldBeTrue();
        pm.CanRemove.ShouldBeFalse();

        var extra = rows[1];
        extra.Source.ShouldBe(MetabaseMemberSource.AdditionalMember);
        extra.InGroup.ShouldBeFalse();
        extra.IsMetabaseUser.ShouldBe(false);
        extra.CanRemove.ShouldBeTrue();

        var gone = rows[2];
        gone.Offboarded.ShouldBeTrue();
        gone.IsMetabaseUser.ShouldBeNull();

        rows[3].Source.ShouldBe(MetabaseMemberSource.Metabase);
        rows[3].CanRemove.ShouldBeTrue();
    }

    [Fact]
    public async Task AddAdditionalMembersAsync_MergesIntoListWithoutDuplicates_ThenSyncs()
    {
        var f = CreateProvisioner(additionalMembers: "existing@gov.bc.ca");

        await f.Provisioner.AddAdditionalMembersAsync(f.Tenant.Id, 22, ["EXISTING@gov.bc.ca", "new@gov.bc.ca"]);

        await f.SettingManager.Received(1).SetAsync(
            MetabaseSettings.UserEmails, "existing@gov.bc.ca,new@gov.bc.ca",
            TenantSettingValueProvider.ProviderName, f.Tenant.Id.ToString(), Arg.Any<bool>());
        await f.MetabaseApiClient.Received(1).GetGroupMembersAsync(22);
    }

    [Fact]
    public async Task RemoveMemberAsync_ActiveProgramManager_Throws()
    {
        var f = CreateProvisioner();
        f.ProgramManagers.Add(CreateUser("pm@gov.bc.ca"));
        f.GroupMembers.Add(GroupMember("pm@gov.bc.ca", 1));

        await Should.ThrowAsync<UserFriendlyException>(() => f.Provisioner.RemoveMemberAsync(f.Tenant.Id, 22, "PM@gov.bc.ca"));

        await f.MetabaseApiClient.DidNotReceiveWithAnyArgs().RemoveGroupMembershipAsync(default);
    }

    [Fact]
    public async Task RemoveMemberAsync_AdditionalMember_TakenOffListAndOutOfGroup()
    {
        var f = CreateProvisioner(additionalMembers: "keep@gov.bc.ca,drop@gov.bc.ca");
        f.GroupMembers.Add(GroupMember("drop@gov.bc.ca", 5));

        await f.Provisioner.RemoveMemberAsync(f.Tenant.Id, 22, "Drop@gov.bc.ca");

        await f.SettingManager.Received(1).SetAsync(
            MetabaseSettings.UserEmails, "keep@gov.bc.ca",
            TenantSettingValueProvider.ProviderName, f.Tenant.Id.ToString(), Arg.Any<bool>());
        await f.MetabaseApiClient.Received(1).RemoveGroupMembershipAsync(5);
    }

    [Fact]
    public async Task RemoveMemberIfNotDesiredAsync_StillAnAdditionalMember_LeavesMembership()
    {
        var f = CreateProvisioner(additionalMembers: "keep@gov.bc.ca");
        f.GroupMembers.Add(GroupMember("keep@gov.bc.ca", 5));

        var removed = await f.Provisioner.RemoveMemberIfNotDesiredAsync(f.Tenant.Id, 22, "keep@gov.bc.ca");

        removed.ShouldBeFalse();
        await f.MetabaseApiClient.DidNotReceiveWithAnyArgs().RemoveGroupMembershipAsync(default);
    }

    [Fact]
    public async Task RemoveMemberIfNotDesiredAsync_DeletedProgramManager_RemovesMembership()
    {
        var f = CreateProvisioner();
        f.ProgramManagers.Add(CreateUser("pm@gov.bc.ca", isDeleted: true));
        f.GroupMembers.Add(GroupMember("pm@gov.bc.ca", 5));

        var removed = await f.Provisioner.RemoveMemberIfNotDesiredAsync(f.Tenant.Id, 22, "pm@gov.bc.ca");

        removed.ShouldBeTrue();
        await f.MetabaseApiClient.Received(1).RemoveGroupMembershipAsync(5);
    }

    [Fact]
    public async Task SyncMembersAsync_RoleAndUserPermissionHolders_AddedAndTracked()
    {
        var f = CreateProvisioner();
        AddRoleHolder(f, "analyst", CreateUser("by-role@gov.bc.ca"));
        AddUserHolder(f, CreateUser("direct@gov.bc.ca"));
        f.MetabaseApiClient.FindUserIdByEmailAsync("by-role@gov.bc.ca").Returns(401);
        f.MetabaseApiClient.FindUserIdByEmailAsync("direct@gov.bc.ca").Returns(402);

        var result = await f.Provisioner.SyncMembersAsync(f.Tenant.Id, 22);

        result.Added.ShouldBe(2);
        await f.MetabaseApiClient.Received(1).AddGroupMemberAsync(22, 401);
        await f.MetabaseApiClient.Received(1).AddGroupMemberAsync(22, 402);
        Tracked(f).ShouldBe("by-role@gov.bc.ca,direct@gov.bc.ca");
    }

    [Fact]
    public async Task SyncMembersAsync_GrantOfAnotherPermissionOrProvider_Ignored()
    {
        var f = CreateProvisioner();
        var user = CreateUser("other@gov.bc.ca");
        f.OtherUsers.Add(user);
        f.Grants.Add(new PermissionGrant(Guid.NewGuid(), "GrantManagerManagement.Default", UserPermissionValueProvider.ProviderName, user.Id.ToString(), f.Tenant.Id));
        f.Grants.Add(new PermissionGrant(Guid.NewGuid(), MetabasePermissions.Access, "C", user.Id.ToString(), f.Tenant.Id));

        await f.Provisioner.SyncMembersAsync(f.Tenant.Id, 22);

        await f.MetabaseApiClient.DidNotReceiveWithAnyArgs().FindUserIdByEmailAsync(default!);
    }

    [Fact]
    public async Task SyncMembersAsync_InactiveOrDeletedPermissionHolder_NotAdded()
    {
        var f = CreateProvisioner();
        AddRoleHolder(f, "analyst", CreateUser("inactive@gov.bc.ca", isActive: false));
        AddUserHolder(f, CreateUser("deleted@gov.bc.ca", isDeleted: true));

        await f.Provisioner.SyncMembersAsync(f.Tenant.Id, 22);

        await f.MetabaseApiClient.DidNotReceiveWithAnyArgs().FindUserIdByEmailAsync(default!);
    }

    [Fact]
    public async Task SyncMembersAsync_TrackedMemberLostPermission_RemovedAndUntracked()
    {
        var f = CreateProvisioner();
        AddUserHolder(f, CreateUser("analyst@gov.bc.ca"));
        f.MetabaseApiClient.FindUserIdByEmailAsync("analyst@gov.bc.ca").Returns(401);
        await f.Provisioner.SyncMembersAsync(f.Tenant.Id, 22);

        f.Grants.Clear();
        f.GroupMembers.Add(GroupMember("analyst@gov.bc.ca", 9));
        var result = await f.Provisioner.SyncMembersAsync(f.Tenant.Id, 22);

        result.Removed.ShouldBe(1);
        await f.MetabaseApiClient.Received(1).RemoveGroupMembershipAsync(9);
        Tracked(f).ShouldBe(string.Empty);
    }

    [Fact]
    public async Task SyncMembersAsync_TrackedMemberLostPermissionButStillAdditionalMember_KeptAndUntracked()
    {
        var f = CreateProvisioner(additionalMembers: "analyst@gov.bc.ca");
        f.Settings[MetabaseSettings.PermissionSyncedEmails] = "analyst@gov.bc.ca";
        f.OtherUsers.Add(CreateUser("analyst@gov.bc.ca"));
        f.GroupMembers.Add(GroupMember("analyst@gov.bc.ca", 9));

        var result = await f.Provisioner.SyncMembersAsync(f.Tenant.Id, 22);

        result.Removed.ShouldBe(0);
        await f.MetabaseApiClient.DidNotReceiveWithAnyArgs().RemoveGroupMembershipAsync(default);
        Tracked(f).ShouldBe(string.Empty);
    }

    [Fact]
    public async Task SyncMembersAsync_PermissionHolderAlreadyInGroup_NeverTrackedSoNeverRemoved()
    {
        var f = CreateProvisioner();
        AddUserHolder(f, CreateUser("analyst@gov.bc.ca"));
        f.GroupMembers.Add(GroupMember("analyst@gov.bc.ca", 9));
        await f.Provisioner.SyncMembersAsync(f.Tenant.Id, 22);

        f.Grants.Clear();
        var result = await f.Provisioner.SyncMembersAsync(f.Tenant.Id, 22);

        Tracked(f).ShouldBeNull();
        result.Removed.ShouldBe(0);
        await f.MetabaseApiClient.DidNotReceiveWithAnyArgs().RemoveGroupMembershipAsync(default);
    }

    [Fact]
    public async Task SyncMembersAsync_ProgramManagerWithPermission_NotTracked()
    {
        var f = CreateProvisioner();
        var pm = CreateUser("pm@gov.bc.ca");
        f.ProgramManagers.Add(pm);
        f.Grants.Add(new PermissionGrant(Guid.NewGuid(), MetabasePermissions.Access, UserPermissionValueProvider.ProviderName, pm.Id.ToString(), f.Tenant.Id));
        f.MetabaseApiClient.FindUserIdByEmailAsync("pm@gov.bc.ca").Returns(401);

        await f.Provisioner.SyncMembersAsync(f.Tenant.Id, 22);

        await f.MetabaseApiClient.Received(1).AddGroupMemberAsync(22, 401);
        Tracked(f).ShouldBeNull();
    }

    [Fact]
    public async Task SyncMembersAsync_RecordsLastMemberSync()
    {
        var f = CreateProvisioner(additionalMembers: "new@gov.bc.ca");
        f.MetabaseApiClient.FindUserIdByEmailAsync("new@gov.bc.ca").Returns(301);

        await f.Provisioner.SyncMembersAsync(f.Tenant.Id, 22);
        var lastSync = await f.Provisioner.GetLastMemberSyncAsync(f.Tenant.Id);

        lastSync.ShouldNotBeNull();
        lastSync.SyncedAt.ShouldBe(Now);
        lastSync.Added.ShouldBe(1);
        lastSync.Error.ShouldBeNull();
    }

    [Fact]
    public async Task SyncMembersAsync_MetabaseFails_RecordsErrorAndRethrows()
    {
        var f = CreateProvisioner();
        f.MetabaseApiClient.GetGroupMembersAsync(22)
            .Throws(new IntegrationServiceException("Metabase is down") { StatusCode = HttpStatusCode.BadGateway });

        await Should.ThrowAsync<IntegrationServiceException>(() => f.Provisioner.SyncMembersAsync(f.Tenant.Id, 22));
        var lastSync = await f.Provisioner.GetLastMemberSyncAsync(f.Tenant.Id);

        lastSync.ShouldNotBeNull();
        lastSync.Error.ShouldBe("Metabase is down");
    }

    [Fact]
    public async Task GetMembersAsync_PermissionHolder_ShownAsPermissionAndNotRemovable()
    {
        var f = CreateProvisioner(additionalMembers: "both@gov.bc.ca");
        f.ProgramManagers.Add(CreateUser("pm@gov.bc.ca"));
        AddRoleHolder(f, "analyst", CreateUser("analyst@gov.bc.ca"));
        AddUserHolder(f, CreateUser("both@gov.bc.ca"));
        f.GroupMembers.Add(GroupMember("pm@gov.bc.ca", 1));
        f.GroupMembers.Add(GroupMember("analyst@gov.bc.ca", 2));
        f.GroupMembers.Add(GroupMember("both@gov.bc.ca", 3));

        var rows = await f.Provisioner.GetMembersAsync(f.Tenant.Id, 22);

        rows.Select(r => r.Email).ShouldBe(["pm@gov.bc.ca", "analyst@gov.bc.ca", "both@gov.bc.ca"]);
        rows[1].Source.ShouldBe(MetabaseMemberSource.Permission);
        rows[1].CanRemove.ShouldBeFalse();
        rows[2].Source.ShouldBe(MetabaseMemberSource.Permission);
        rows[2].CanRemove.ShouldBeFalse();
    }

    [Fact]
    public async Task RemoveMemberAsync_PermissionHolder_Throws()
    {
        var f = CreateProvisioner();
        AddUserHolder(f, CreateUser("analyst@gov.bc.ca"));
        f.GroupMembers.Add(GroupMember("analyst@gov.bc.ca", 1));

        await Should.ThrowAsync<UserFriendlyException>(() => f.Provisioner.RemoveMemberAsync(f.Tenant.Id, 22, "analyst@gov.bc.ca"));

        await f.MetabaseApiClient.DidNotReceiveWithAnyArgs().RemoveGroupMembershipAsync(default);
    }

    [Fact]
    public async Task RemoveMemberIfNotDesiredAsync_PermissionHolder_LeavesMembership()
    {
        var f = CreateProvisioner();
        AddRoleHolder(f, "analyst", CreateUser("analyst@gov.bc.ca"));
        f.GroupMembers.Add(GroupMember("analyst@gov.bc.ca", 5));

        var removed = await f.Provisioner.RemoveMemberIfNotDesiredAsync(f.Tenant.Id, 22, "analyst@gov.bc.ca");

        removed.ShouldBeFalse();
        await f.MetabaseApiClient.DidNotReceiveWithAnyArgs().RemoveGroupMembershipAsync(default);
    }
}
