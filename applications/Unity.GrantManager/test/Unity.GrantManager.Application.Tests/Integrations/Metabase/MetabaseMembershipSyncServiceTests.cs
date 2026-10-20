using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Unity.GrantManager.Tenants.PostCreation.Steps;
using Unity.Modules.Shared.PostTenantCreation;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.MultiTenancy;
using Volo.Abp.PermissionManagement;
using Volo.Abp.Security.Encryption;
using Volo.Abp.SettingManagement;
using Volo.Abp.TenantManagement;
using Volo.Abp.Timing;
using Xunit;

namespace Unity.GrantManager.Integrations.Metabase;

public class MetabaseMembershipSyncServiceTests
{
    // A class, not a substitute: CanExecuteAsync is a default interface method, which
    // NSubstitute can't intercept.
    private sealed class FakeStep : IPostTenantCreationStep
    {
        public Dictionary<Guid, PostTenantCreationStepCheck> Checks { get; } = [];
        public List<Guid> Checked { get; } = [];

        public int Order => 1;
        public string Key => MetabaseTenantRegistrationStep.StepKey;
        public string StepName => "Metabase";
        public bool ContinueOnError => true;

        public Task<PostTenantCreationStepCheck> CanExecuteAsync(Guid tenantId)
        {
            Checked.Add(tenantId);
            return Task.FromResult(Checks.GetValueOrDefault(tenantId, PostTenantCreationStepCheck.Run));
        }

        public Task ExecuteAsync(Guid tenantId) => Task.CompletedTask;
    }

    // Tenant's constructors are all non-public; see MetabaseTenantProvisionerTests.
    private static Tenant CreateTenant(string name, int? groupId)
    {
        var ctor = typeof(Tenant).GetConstructor(
            BindingFlags.NonPublic | BindingFlags.Instance,
            null, [typeof(Guid), typeof(string), typeof(string)], null)!;
        var tenant = (Tenant)ctor.Invoke([Guid.NewGuid(), name, name.ToUpperInvariant()]);
        if (groupId.HasValue)
        {
            tenant.SetProperty(MetabaseTenantProvisioner.GroupIdProperty, groupId.Value);
        }
        return tenant;
    }

    private static (MetabaseMembershipSyncService Service, MetabaseTenantProvisioner Provisioner, FakeStep Step)
        CreateService(params Tenant[] tenants)
    {
        var tenantRepository = Substitute.For<ITenantRepository>();
        tenantRepository.GetListAsync().ReturnsForAnyArgs(new List<Tenant>(tenants));

        var currentTenant = Substitute.For<ICurrentTenant>();
        currentTenant.Change(Arg.Any<Guid?>(), Arg.Any<string?>()).Returns(Substitute.For<IDisposable>());

        var step = new FakeStep();

        // Every provisioner method is virtual, so a substitute stands in for the Metabase work.
        var provisioner = Substitute.For<MetabaseTenantProvisioner>(
            Substitute.For<IMetabaseApiClient>(), tenantRepository, Substitute.For<IIdentityUserRepository>(),
            Substitute.For<IRepository<PermissionGrant, Guid>>(),
            Substitute.For<IStringEncryptionService>(), Substitute.For<ISettingManager>(), currentTenant,
            Substitute.For<IDataFilter>(), Substitute.For<IClock>(), Options.Create(new MetabaseOptions()),
            Substitute.For<ILogger<MetabaseTenantProvisioner>>());
        provisioner.SyncMembersAsync(Arg.Any<Guid>(), Arg.Any<int>()).Returns(new MetabaseMemberSyncResultDto());

        var service = new MetabaseMembershipSyncService(
            tenantRepository, [step], provisioner, currentTenant, Substitute.For<ILogger<MetabaseMembershipSyncService>>());

        return (service, provisioner, step);
    }

    [Fact]
    public async Task SyncAllTenantsAsync_SyncsSetUpTenants_SkipsTenantsNotSetUp()
    {
        var setUp = CreateTenant("AG-MARB", groupId: 22);
        var notSetUp = CreateTenant("AG-NEW", groupId: null);
        var (service, provisioner, step) = CreateService(setUp, notSetUp);

        var (synced, skipped, failed) = await service.SyncAllTenantsAsync();

        (synced, skipped, failed).ShouldBe((1, 1, 0));
        await provisioner.Received(1).SyncMembersAsync(setUp.Id, 22);
        await provisioner.DidNotReceive().SyncMembersAsync(notSetUp.Id, Arg.Any<int>());
        step.Checked.ShouldBe([setUp.Id]);
    }

    [Fact]
    public async Task SyncAllTenantsAsync_SetupCantRun_Skipped()
    {
        var tenant = CreateTenant("AG-MARB", groupId: 22);
        var (service, provisioner, step) = CreateService(tenant);
        step.Checks[tenant.Id] = PostTenantCreationStepCheck.Skip("Metabase is not enabled for this environment.");

        var (synced, skipped, failed) = await service.SyncAllTenantsAsync();

        (synced, skipped, failed).ShouldBe((0, 1, 0));
        await provisioner.DidNotReceiveWithAnyArgs().SyncMembersAsync(default, default);
    }

    [Fact]
    public async Task SyncAllTenantsAsync_OneTenantFails_OthersStillSync()
    {
        var failing = CreateTenant("AG-FAIL", groupId: 21);
        var working = CreateTenant("AG-MARB", groupId: 22);
        var (service, provisioner, _) = CreateService(failing, working);
        provisioner.SyncMembersAsync(failing.Id, 21)
            .Returns(Task.FromException<MetabaseMemberSyncResultDto>(new InvalidOperationException("boom")));

        var (synced, skipped, failed) = await service.SyncAllTenantsAsync();

        (synced, skipped, failed).ShouldBe((1, 0, 1));
        await provisioner.Received(1).SyncMembersAsync(working.Id, 22);
    }
}
