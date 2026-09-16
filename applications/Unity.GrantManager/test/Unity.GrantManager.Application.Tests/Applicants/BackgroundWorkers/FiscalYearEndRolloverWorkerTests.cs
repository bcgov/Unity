using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Quartz;
using Shouldly;
using Unity.GrantManager.Applicants.BackgroundWorkers;
using Unity.GrantManager.Applications;
using Volo.Abp.MultiTenancy;
using Volo.Abp.SettingManagement;
using Volo.Abp.TenantManagement;
using Xunit;

namespace Unity.GrantManager.Applicants;

public class FiscalYearEndRolloverWorkerTests
{
    [Fact]
    public async Task ShouldChangeCurrentTenantContext_ForEachTenant()
    {
        var tenantOne = CreateTenant("Tenant One");
        var tenantTwo = CreateTenant("Tenant Two");
        var currentTenant = CreateCurrentTenant();
        var applicantRepository = Substitute.For<IApplicantRepository>();
        applicantRepository.RollOverFiscalYearEndAsync().Returns(new FiscalYearEndRolloverResult { RowsAffected = 1 });

        var worker = CreateWorker(currentTenant, CreateTenantRepository(tenantOne, tenantTwo), applicantRepository);

        await worker.Execute(Substitute.For<IJobExecutionContext>());

        currentTenant.Received(1).Change(tenantOne.Id, tenantOne.Name);
        currentTenant.Received(1).Change(tenantTwo.Id, tenantTwo.Name);
        await applicantRepository.Received(2).RollOverFiscalYearEndAsync();
    }

    [Fact]
    public async Task ShouldSkipTenant_WhenColumnsAreMissing()
    {
        var tenantOne = CreateTenant("Tenant One");
        var tenantTwo = CreateTenant("Tenant Two");
        var currentTenant = CreateCurrentTenant();
        var applicantRepository = Substitute.For<IApplicantRepository>();
        applicantRepository.RollOverFiscalYearEndAsync().Returns(
            new FiscalYearEndRolloverResult { MissingColumns = ["FiscalMonth", "FiscalDay"] },
            new FiscalYearEndRolloverResult { RowsAffected = 3 });

        var worker = CreateWorker(currentTenant, CreateTenantRepository(tenantOne, tenantTwo), applicantRepository);

        // Should not throw despite the first tenant missing columns, and both tenants are still visited.
        await worker.Execute(Substitute.For<IJobExecutionContext>());

        await applicantRepository.Received(2).RollOverFiscalYearEndAsync();
        currentTenant.Received(1).Change(tenantOne.Id, tenantOne.Name);
        currentTenant.Received(1).Change(tenantTwo.Id, tenantTwo.Name);
    }

    [Fact]
    public async Task ShouldReportRowsAffected_WhenRolloverSucceeds()
    {
        var tenant = CreateTenant("Tenant One");
        var currentTenant = CreateCurrentTenant();
        var applicantRepository = Substitute.For<IApplicantRepository>();
        applicantRepository.RollOverFiscalYearEndAsync().Returns(new FiscalYearEndRolloverResult { RowsAffected = 42 });

        var worker = CreateWorker(currentTenant, CreateTenantRepository(tenant), applicantRepository);

        await worker.Execute(Substitute.For<IJobExecutionContext>());

        var result = await applicantRepository.RollOverFiscalYearEndAsync();
        result.RowsAffected.ShouldBe(42);
        result.ColumnsAvailable.ShouldBeTrue();
    }

    [Fact]
    public async Task ShouldContinueProcessingRemainingTenants_WhenOneTenantThrows()
    {
        var tenantOne = CreateTenant("Tenant One");
        var tenantTwo = CreateTenant("Tenant Two");
        var currentTenant = CreateCurrentTenant();
        var applicantRepository = Substitute.For<IApplicantRepository>();
        applicantRepository.RollOverFiscalYearEndAsync().Returns(
            _ => throw new InvalidOperationException("Connection to tenant database failed."),
            _ => Task.FromResult(new FiscalYearEndRolloverResult { RowsAffected = 5 }));

        var worker = CreateWorker(currentTenant, CreateTenantRepository(tenantOne, tenantTwo), applicantRepository);

        await Should.NotThrowAsync(() => worker.Execute(Substitute.For<IJobExecutionContext>()));

        currentTenant.Received(1).Change(tenantOne.Id, tenantOne.Name);
        currentTenant.Received(1).Change(tenantTwo.Id, tenantTwo.Name);
        await applicantRepository.Received(2).RollOverFiscalYearEndAsync();
    }

    private static FiscalYearEndRolloverWorker CreateWorker(
        ICurrentTenant currentTenant,
        ITenantRepository tenantRepository,
        IApplicantRepository applicantRepository)
    {
        var logger = Substitute.For<ILogger<FiscalYearEndRolloverWorker>>();
        var settingManager = Substitute.For<ISettingManager>();

        return new FiscalYearEndRolloverWorker(logger, currentTenant, tenantRepository, applicantRepository, settingManager);
    }

    private static ICurrentTenant CreateCurrentTenant()
    {
        var currentTenant = Substitute.For<ICurrentTenant>();
        currentTenant.Change(Arg.Any<Guid?>(), Arg.Any<string>()).Returns(Substitute.For<IDisposable>());
        return currentTenant;
    }

    private static ITenantRepository CreateTenantRepository(params Tenant[] tenants)
    {
        var tenantRepository = Substitute.For<ITenantRepository>();
        tenantRepository.GetListAsync().Returns([.. tenants]);
        return tenantRepository;
    }

    // Tenant's constructors are all non-public (ABP requires going through ITenantManager to
    // create one) - reflection is the standard workaround for exercising it in a plain unit test.
    private static Tenant CreateTenant(string name)
    {
        var constructor = typeof(Tenant).GetConstructor(
            BindingFlags.NonPublic | BindingFlags.Instance,
            null, [typeof(Guid), typeof(string), typeof(string)], null)
            ?? throw new InvalidOperationException("Expected ABP Tenant constructor was not found.");
        return (Tenant)constructor.Invoke([Guid.NewGuid(), name, name.ToUpperInvariant()]);
    }
}
