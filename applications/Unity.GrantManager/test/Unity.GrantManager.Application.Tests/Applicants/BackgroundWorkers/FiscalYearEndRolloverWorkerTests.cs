using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Quartz;
using Shouldly;
using Unity.GrantManager.Applicants.BackgroundWorkers;
using Unity.GrantManager.Applications;
using Volo.Abp.Auditing;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Security.Claims;
using Volo.Abp.SettingManagement;
using Volo.Abp.TenantManagement;
using Volo.Abp.Uow;
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

    [Fact]
    public async Task ShouldPersistSeparateAuditLogs_WhenProcessingMultipleTenants()
    {
        var tenantOne = CreateTenant("Tenant One");
        var tenantTwo = CreateTenant("Tenant Two");
        var currentTenant = CreateCurrentTenant();
        var applicantRepository = Substitute.For<IApplicantRepository>();
        var auditingManager = CreateAuditingManager(out var savedLogs);
        var auditingStore = Substitute.For<IAuditingStore>();
        auditingStore.SaveAsync(Arg.Any<AuditLogInfo>()).Returns(callInfo =>
        {
            savedLogs.Add(callInfo.Arg<AuditLogInfo>());
            return Task.CompletedTask;
        });
        applicantRepository.RollOverFiscalYearEndAsync().Returns(_ =>
        {
            auditingManager.Current!.Log.EntityChanges.Add(new EntityChangeInfo());
            return Task.FromResult(new FiscalYearEndRolloverResult { RowsAffected = 1 });
        });

        var worker = CreateWorker(
            currentTenant,
            CreateTenantRepository(tenantOne, tenantTwo),
            applicantRepository,
            auditingManager,
            auditingStore);

        await worker.Execute(Substitute.For<IJobExecutionContext>());

        savedLogs.Count.ShouldBe(2);
        savedLogs.Select(log => log.TenantId).ShouldBe([tenantOne.Id, tenantTwo.Id]);
        savedLogs.ShouldAllBe(log => log.EntityChanges.Count == 1);
    }

    private static FiscalYearEndRolloverWorker CreateWorker(
        ICurrentTenant currentTenant,
        ITenantRepository tenantRepository,
        IApplicantRepository applicantRepository,
        IAuditingManager? auditingManager = null,
        IAuditingStore? auditingStore = null)
    {
        var logger = Substitute.For<ILogger<FiscalYearEndRolloverWorker>>();
        var settingManager = Substitute.For<ISettingManager>();
        var serviceProvider = CreateServiceProvider(currentTenant, applicantRepository, auditingManager, auditingStore);

        var worker = new FiscalYearEndRolloverWorker(logger, serviceProvider, tenantRepository, settingManager)
        {
            ServiceProvider = serviceProvider
        };
        return worker;
    }

    private static IServiceProvider CreateServiceProvider(
        ICurrentTenant currentTenant,
        IApplicantRepository applicantRepository,
        IAuditingManager? auditingManager = null,
        IAuditingStore? auditingStore = null)
    {
        var uowManager = Substitute.For<IUnitOfWorkManager>();
        uowManager.Begin(Arg.Any<AbpUnitOfWorkOptions>(), Arg.Any<bool>()).Returns(Substitute.For<IUnitOfWork>());

        var services = new ServiceCollection();
        services.AddSingleton(currentTenant);
        services.AddSingleton(Substitute.For<ICurrentPrincipalAccessor>());
        services.AddSingleton(uowManager);
        services.AddSingleton(applicantRepository);
        services.AddSingleton(auditingManager ?? CreateAuditingManager(out _));
        services.AddSingleton(auditingStore ?? Substitute.For<IAuditingStore>());
        return services.BuildServiceProvider();
    }

    private static IAuditingManager CreateAuditingManager(out List<AuditLogInfo> auditLogs)
    {
        auditLogs = [];
        var scopes = new Stack<IAuditLogScope>();
        var auditingManager = Substitute.For<IAuditingManager>();
        auditingManager.Current.Returns(_ => scopes.TryPeek(out var scope) ? scope : null);
        auditingManager.BeginScope().Returns(_ =>
        {
            var scope = Substitute.For<IAuditLogScope>();
            scope.Log.Returns(new AuditLogInfo());
            scopes.Push(scope);

            var disposable = Substitute.For<IAuditLogSaveHandle>();
            disposable.When(x => x.Dispose()).Do(_ => scopes.Pop());
            return disposable;
        });
        return auditingManager;
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
