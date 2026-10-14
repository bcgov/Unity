using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using Unity.GrantManager.Integrations.Exceptions;
using Unity.GrantManager.Tenants.PostCreation;
using Unity.GrantManager.Tenants.PostCreation.Steps;
using Unity.Modules.Shared.PostTenantCreation;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.MultiTenancy;
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

        // Registered out of order, as DI may return them.
        var service = new MetabaseTenantAppService(
            tenantRepository, [metabaseStep, otherStep], backgroundJobManager, metabaseApiClient)
        {
            LazyServiceProvider = lazyServices
        };

        return new Fixture(service, metabaseApiClient, backgroundJobManager, tenant);
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
}
