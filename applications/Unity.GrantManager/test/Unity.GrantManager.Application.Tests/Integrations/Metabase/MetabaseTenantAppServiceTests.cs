using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using Unity.GrantManager.Integrations.Exceptions;
using Unity.GrantManager.Tenants.PostCreation;
using Unity.GrantManager.Tenants.PostCreation.Steps;
using Unity.Modules.Shared.PostTenantCreation;
using Volo.Abp;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
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

public class MetabaseTenantAppServiceTests
{
    private sealed record Fixture(
        MetabaseTenantAppService Service,
        IMetabaseApiClient MetabaseApiClient,
        IBackgroundJobManager BackgroundJobManager,
        MetabaseTenantProvisioner Provisioner,
        Tenant Tenant);

    // Tenant's constructors are all non-public (ABP requires going through ITenantManager to
    // create one) - reflection is the standard workaround in a plain, DB-less unit test.
    private static Tenant CreateTenant(string name)
    {
        var ctor = typeof(Tenant).GetConstructor(
            BindingFlags.NonPublic | BindingFlags.Instance,
            null, [typeof(Guid), typeof(string), typeof(string)], null)!;
        return (Tenant)ctor.Invoke([Guid.NewGuid(), name, name.ToUpperInvariant()]);
    }

    // A class, not a substitute: CanExecuteAsync is a default interface method, which
    // NSubstitute can't intercept.
    private sealed class FakeStep(int order, string key, PostTenantCreationStepCheck check) : IPostTenantCreationStep
    {
        public int Order { get; } = order;
        public string Key { get; } = key;
        public string StepName { get; } = key;
        public bool ContinueOnError => true;

        public Task<PostTenantCreationStepCheck> CanExecuteAsync(Guid tenantId) => Task.FromResult(check);

        public Task ExecuteAsync(Guid tenantId) => Task.CompletedTask;
    }

    private static Fixture CreateService(PostTenantCreationStepCheck? check = null)
    {
        var tenant = CreateTenant("AG-MARB");
        var tenantRepository = Substitute.For<ITenantRepository>();
        tenantRepository.GetAsync(tenant.Id, Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(tenant);

        var otherStep = new FakeStep(0, "Other", PostTenantCreationStepCheck.Run);
        var metabaseStep = new FakeStep(1, MetabaseTenantRegistrationStep.StepKey, check ?? PostTenantCreationStepCheck.Run);

        var metabaseApiClient = Substitute.For<IMetabaseApiClient>();
        metabaseApiClient.CheckConnectionAsync().Returns(true);
        metabaseApiClient.GetSiteUrlAsync().Returns("https://metabase.example/");

        var backgroundJobManager = Substitute.For<IBackgroundJobManager>();

        var currentTenant = Substitute.For<ICurrentTenant>();
        currentTenant.Change(Arg.Any<Guid?>(), Arg.Any<string?>()).Returns(Substitute.For<IDisposable>());
        var clock = Substitute.For<IClock>();
        clock.Now.Returns(new DateTime(2026, 10, 14, 12, 0, 0, DateTimeKind.Utc));
        var lazyServices = Substitute.For<IAbpLazyServiceProvider>();
        lazyServices.LazyGetRequiredService<ICurrentTenant>().Returns(currentTenant);
        lazyServices.LazyGetRequiredService<IClock>().Returns(clock);

        // Every provisioner method is virtual, so a substitute stands in for the Metabase work.
        var provisioner = Substitute.For<MetabaseTenantProvisioner>(
            metabaseApiClient, tenantRepository, Substitute.For<IIdentityUserRepository>(),
            Substitute.For<IRepository<PermissionGrant, Guid>>(),
            Substitute.For<IStringEncryptionService>(), Substitute.For<ISettingManager>(), currentTenant,
            Substitute.For<IDataFilter>(), Substitute.For<IClock>(), Options.Create(new MetabaseOptions()),
            Substitute.For<ILogger<MetabaseTenantProvisioner>>());

        // Registered out of order, as DI may return them.
        var service = new MetabaseTenantAppService(
            tenantRepository, [metabaseStep, otherStep], backgroundJobManager, metabaseApiClient, provisioner)
        {
            LazyServiceProvider = lazyServices
        };

        return new Fixture(service, metabaseApiClient, backgroundJobManager, provisioner, tenant);
    }

    [Fact]
    public async Task GetAsync_SetUpTenant_ReturnsStoredIdsStatusAndConnection()
    {
        var f = CreateService();
        f.Tenant.SetProperty(MetabaseTenantProvisioner.DatabaseIdProperty, 11);
        f.Tenant.SetProperty(MetabaseTenantProvisioner.GroupIdProperty, 22);
        f.Tenant.SetProperty(MetabaseTenantProvisioner.CollectionIdProperty, 33);
        f.Tenant.SetPostTenantCreationStepStatus(
            MetabaseTenantRegistrationStep.StepKey, "Metabase", PostTenantCreationStepStatus.Success, null, DateTime.UtcNow);

        var status = await f.Service.GetAsync(f.Tenant.Id);

        status.IsSetUp.ShouldBeTrue();
        status.DatabaseId.ShouldBe(11);
        status.GroupId.ShouldBe(22);
        status.CollectionId.ShouldBe(33);
        status.Status.ShouldBe("Success");
        status.CanRun.ShouldBeTrue();
        status.ConnectionOk.ShouldBe(true);
        status.ConnectionMessage.ShouldBeNull();
        status.MetabaseUrl.ShouldBe("https://metabase.example");
        status.ConcurrencyStamp.ShouldBe(f.Tenant.ConcurrencyStamp);
    }

    [Fact]
    public async Task GetAsync_NotSetUp_HasNoIds()
    {
        var f = CreateService();

        var status = await f.Service.GetAsync(f.Tenant.Id);

        status.IsSetUp.ShouldBeFalse();
        status.DatabaseId.ShouldBeNull();
        status.Status.ShouldBeNull();
    }

    [Fact]
    public async Task GetAsync_StepCannotRun_ReturnsReasonAndSkipsConnectionCheck()
    {
        var f = CreateService(PostTenantCreationStepCheck.Fail("no API key"));

        var status = await f.Service.GetAsync(f.Tenant.Id);

        status.CanRun.ShouldBeFalse();
        status.NotRunReason.ShouldBe("no API key");
        status.NotRunIsFailure.ShouldBeTrue();
        status.ConnectionOk.ShouldBeNull();
        await f.MetabaseApiClient.DidNotReceive().CheckConnectionAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAsync_StepSkipped_IsNotAFailure()
    {
        var f = CreateService(PostTenantCreationStepCheck.Skip("no Reporting"));

        var status = await f.Service.GetAsync(f.Tenant.Id);

        status.CanRun.ShouldBeFalse();
        status.NotRunIsFailure.ShouldBeFalse();
    }

    [Fact]
    public async Task GetAsync_MetabaseRejectsKey_ReportsConnectionFailure()
    {
        var f = CreateService();
        f.MetabaseApiClient.CheckConnectionAsync().ThrowsAsync(
            new IntegrationServiceException("Metabase API call to '/api/user/current' failed with status Unauthorized") { StatusCode = HttpStatusCode.Unauthorized });

        var status = await f.Service.GetAsync(f.Tenant.Id);

        status.ConnectionOk.ShouldBe(false);
        status.ConnectionMessage!.ShouldContain("Unauthorized");
    }

    [Fact]
    public async Task GetAsync_KeyIsNotAnAdministrators_WarnsButConnects()
    {
        var f = CreateService();
        f.MetabaseApiClient.CheckConnectionAsync().Returns(false);

        var status = await f.Service.GetAsync(f.Tenant.Id);

        status.ConnectionOk.ShouldBe(true);
        status.ConnectionMessage.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task SetupAsync_MarksWaitingAndEnqueuesJobAtMetabaseStepIndex()
    {
        var f = CreateService();
        PostTenantCreationStepArgs? enqueued = null;
        await f.BackgroundJobManager.EnqueueAsync(
            Arg.Do<PostTenantCreationStepArgs>(a => enqueued = a), Arg.Any<BackgroundJobPriority>(), Arg.Any<TimeSpan?>());

        await f.Service.SetupAsync(f.Tenant.Id);

        enqueued.ShouldNotBeNull();
        enqueued.TenantId.ShouldBe(f.Tenant.Id);
        enqueued.StepIndex.ShouldBe(1);
        f.Tenant.GetPostTenantCreationSections().ShouldHaveSingleItem().Status.ShouldBe(PostTenantCreationStepStatus.Waiting);
    }

    [Fact]
    public async Task ResyncIfSetUpAsync_SetUpTenant_QueuesResync()
    {
        var f = CreateService();
        f.Tenant.SetProperty(MetabaseTenantProvisioner.DatabaseIdProperty, 46);

        var queued = await f.Service.ResyncIfSetUpAsync(f.Tenant.Id);

        queued.ShouldBeTrue();
        await f.BackgroundJobManager.Received(1).EnqueueAsync(
            Arg.Is<PostTenantCreationStepArgs>(a => a.TenantId == f.Tenant.Id && a.StepIndex == 1),
            Arg.Any<BackgroundJobPriority>(), Arg.Any<TimeSpan?>());
    }

    [Fact]
    public async Task ResyncIfSetUpAsync_NotSetUp_DoesNothing()
    {
        var f = CreateService();

        var queued = await f.Service.ResyncIfSetUpAsync(f.Tenant.Id);

        queued.ShouldBeFalse();
        await f.BackgroundJobManager.DidNotReceiveWithAnyArgs().EnqueueAsync<PostTenantCreationStepArgs>(default!);
        f.Tenant.GetPostTenantCreationSections().ShouldBeEmpty();
    }

    [Fact]
    public async Task ResyncIfSetUpAsync_SetupCannotRun_DoesNothing()
    {
        var f = CreateService(PostTenantCreationStepCheck.Skip("Metabase is not enabled for this environment."));
        f.Tenant.SetProperty(MetabaseTenantProvisioner.DatabaseIdProperty, 46);

        var queued = await f.Service.ResyncIfSetUpAsync(f.Tenant.Id);

        queued.ShouldBeFalse();
        await f.BackgroundJobManager.DidNotReceiveWithAnyArgs().EnqueueAsync<PostTenantCreationStepArgs>(default!);
    }

    [Fact]
    public async Task SyncSchemaAsync_SetUpTenant_SyncsStoredDatabase()
    {
        var f = CreateService();
        f.Tenant.SetProperty(MetabaseTenantProvisioner.DatabaseIdProperty, 11);

        await f.Service.SyncSchemaAsync(f.Tenant.Id);

        await f.MetabaseApiClient.Received(1).SyncDatabaseSchemaAsync(11, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RescanValuesAsync_SetUpTenant_RescansStoredDatabase()
    {
        var f = CreateService();
        f.Tenant.SetProperty(MetabaseTenantProvisioner.DatabaseIdProperty, 11);

        await f.Service.RescanValuesAsync(f.Tenant.Id);

        await f.MetabaseApiClient.Received(1).RescanDatabaseValuesAsync(11, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SyncSchemaAsync_NotSetUp_ThrowsWithoutCallingMetabase()
    {
        var f = CreateService();

        await Should.ThrowAsync<UserFriendlyException>(() => f.Service.SyncSchemaAsync(f.Tenant.Id));

        await f.MetabaseApiClient.DidNotReceiveWithAnyArgs().SyncDatabaseSchemaAsync(default);
    }

    [Fact]
    public async Task RescanValuesAsync_DatabaseDeletedInMetabase_SuggestsResync()
    {
        var f = CreateService();
        f.Tenant.SetProperty(MetabaseTenantProvisioner.DatabaseIdProperty, 11);
        f.MetabaseApiClient.RescanDatabaseValuesAsync(11, Arg.Any<CancellationToken>()).ThrowsAsync(
            new IntegrationServiceException("Metabase API call to '/api/database/11/rescan_values' failed with status NotFound") { StatusCode = HttpStatusCode.NotFound });

        var ex = await Should.ThrowAsync<UserFriendlyException>(() => f.Service.RescanValuesAsync(f.Tenant.Id));

        ex.Message.ShouldContain("Resync");
    }

    [Fact]
    public async Task GetMembersAsync_NotSetUp_ThrowsUserFriendly()
    {
        var f = CreateService();

        await Should.ThrowAsync<UserFriendlyException>(() => f.Service.GetMembersAsync(f.Tenant.Id));
    }

    [Fact]
    public async Task SyncMembersAsync_UsesStoredGroupId()
    {
        var f = CreateService();
        f.Tenant.SetProperty(MetabaseTenantProvisioner.GroupIdProperty, 140);
        f.Provisioner.SyncMembersAsync(f.Tenant.Id, 140).Returns(new MetabaseMemberSyncResultDto { Added = 2 });

        var result = await f.Service.SyncMembersAsync(f.Tenant.Id);

        result.Added.ShouldBe(2);
    }

    [Fact]
    public async Task SyncMembersAsync_GroupDeletedInMetabase_ThrowsUserFriendlyWithResync()
    {
        var f = CreateService();
        f.Tenant.SetProperty(MetabaseTenantProvisioner.GroupIdProperty, 140);
        f.Provisioner.SyncMembersAsync(f.Tenant.Id, 140).ThrowsAsync(
            new IntegrationServiceException("gone") { StatusCode = HttpStatusCode.NotFound });

        var ex = await Should.ThrowAsync<UserFriendlyException>(() => f.Service.SyncMembersAsync(f.Tenant.Id));

        ex.Message.ShouldContain("group #140");
    }

    [Fact]
    public async Task AddMembersAsync_SplitsOnCommasSemicolonsAndNewLines()
    {
        var f = CreateService();
        f.Tenant.SetProperty(MetabaseTenantProvisioner.GroupIdProperty, 140);

        await f.Service.AddMembersAsync(f.Tenant.Id, new MetabaseAddMembersDto { Emails = "a@gov.bc.ca; b@gov.bc.ca\nc@gov.bc.ca," });

        await f.Provisioner.Received(1).AddAdditionalMembersAsync(f.Tenant.Id, 140,
            Arg.Is<IEnumerable<string>>(e => e.SequenceEqual(new[] { "a@gov.bc.ca", "b@gov.bc.ca", "c@gov.bc.ca" })));
    }

    [Fact]
    public async Task AddMembersAsync_InvalidEmail_ThrowsWithoutChangingAnything()
    {
        var f = CreateService();
        f.Tenant.SetProperty(MetabaseTenantProvisioner.GroupIdProperty, 140);

        var ex = await Should.ThrowAsync<UserFriendlyException>(() =>
            f.Service.AddMembersAsync(f.Tenant.Id, new MetabaseAddMembersDto { Emails = "ok@gov.bc.ca,not-an-email" }));

        ex.Message.ShouldContain("not-an-email");
        await f.Provisioner.DidNotReceiveWithAnyArgs().AddAdditionalMembersAsync(default, default, default!);
    }

    [Fact]
    public async Task RemoveMemberAsync_PassesTrimmedEmail()
    {
        var f = CreateService();
        f.Tenant.SetProperty(MetabaseTenantProvisioner.GroupIdProperty, 140);

        await f.Service.RemoveMemberAsync(f.Tenant.Id, new MetabaseRemoveMemberDto { Email = " x@gov.bc.ca " });

        await f.Provisioner.Received(1).RemoveMemberAsync(f.Tenant.Id, 140, "x@gov.bc.ca");
    }
}
