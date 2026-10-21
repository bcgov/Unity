using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Unity.Reporting.Configuration;
using Unity.Reporting.Domain.Configuration;
using Volo.Abp;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.DependencyInjection;
using Volo.Abp.MultiTenancy;
using Volo.Abp.SettingManagement;
using Volo.Abp.TenantManagement;
using Xunit;
using Xunit.Abstractions;

namespace Unity.Reporting.Application.Tests.Configuration;

public class TenantViewRoleAppServiceTests : ReportingApplicationTestBase<ReportingApplicationTestModule>
{
    private const string SettingName = "GrantManager.Reporting.TenantViewRole";

    private readonly ITenantRepository _tenantRepository;
    private readonly ISettingManager _settingManager;
    private readonly IBackgroundJobManager _backgroundJobManager;
    private readonly IReportColumnsMapRepository _reportColumnsMapRepository;
    private readonly ICurrentTenant _currentTenant;
    private readonly TenantViewRoleAppService _service;

    public TenantViewRoleAppServiceTests(ITestOutputHelper outputHelper) : base(outputHelper)
    {
        _tenantRepository = Substitute.For<ITenantRepository>();
        _settingManager = Substitute.For<ISettingManager>();
        _backgroundJobManager = Substitute.For<IBackgroundJobManager>();
        _reportColumnsMapRepository = Substitute.For<IReportColumnsMapRepository>();
        _currentTenant = Substitute.For<ICurrentTenant>();
        _currentTenant.Change(Arg.Any<Guid?>()).Returns(Substitute.For<IDisposable>());

        _service = new TenantViewRoleAppService(
            _tenantRepository, new TenantViewRoleManager(_tenantRepository, _settingManager, _currentTenant),
            _backgroundJobManager, _reportColumnsMapRepository, _currentTenant);

        SetupServicePropertiesForTesting(_service, Substitute.For<ILogger<TenantViewRoleAppService>>());
    }

    // Tenant's constructors are all non-public (ABP requires going through ITenantManager to
    // create one) - reflection is the standard workaround for exercising it in a plain unit test.
    private static Tenant CreateTenant(string name)
    {
        var ctor = typeof(Tenant).GetConstructor(
            BindingFlags.NonPublic | BindingFlags.Instance,
            null, [typeof(Guid), typeof(string), typeof(string)], null)!;
        return (Tenant)ctor.Invoke([Guid.NewGuid(), name, name.ToUpperInvariant()]);
    }

    // Mirrors ReportMappingServiceTests' approach: TenantViewRoleAppService is constructed
    // directly (not resolved via DI) so tests don't need a real Postgres-backed EF Core context -
    // its Logger property (from ApplicationService's LazyServiceProvider) is wired up manually.
    private static void SetupServicePropertiesForTesting(object service, ILogger logger)
    {
        var mockLazyServiceProvider = Substitute.For<IAbpLazyServiceProvider>();
        mockLazyServiceProvider.LazyGetService<ILogger>(Arg.Any<Func<IServiceProvider, ILogger>>())
            .Returns(logger);

        var lazyServiceProviderProperty = typeof(TenantViewRoleAppService)
            .GetProperty("LazyServiceProvider", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        if (lazyServiceProviderProperty == null)
        {
            var currentType = typeof(TenantViewRoleAppService).BaseType;
            while (currentType != null && lazyServiceProviderProperty == null)
            {
                lazyServiceProviderProperty = currentType.GetProperty("LazyServiceProvider",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                currentType = currentType.BaseType;
            }
        }

        if (lazyServiceProviderProperty != null && lazyServiceProviderProperty.CanWrite)
        {
            lazyServiceProviderProperty.SetValue(service, mockLazyServiceProvider);
        }
        else
        {
            throw new InvalidOperationException("Could not find or set LazyServiceProvider property");
        }
    }

    [Fact]
    public async Task GetAsync_TenantHasLicencePlate_NoSavedRole_UsesLicencePlateReadOnlyRole()
    {
        var tenant = CreateTenant("acme");
        tenant.ExtraProperties["LicencePlate"] = "T_ABC123";
        _tenantRepository.GetAsync(tenant.Id).Returns(tenant);
        _settingManager.GetOrNullAsync(SettingName, "T", tenant.Id.ToString()).Returns((string?)null);
        _reportColumnsMapRepository.RoleExistsAsync("T_ABC123_readonly").Returns(true);
        _reportColumnsMapRepository.RoleExistsAsync("T_ABC123").Returns(false);

        var result = await _service.GetAsync(tenant.Id);

        result.LicencePlate.ShouldBe("T_ABC123");
        result.ExpectedReadOnlyRole.ShouldBe("T_ABC123_readonly");
        result.ViewRole.ShouldBe("T_ABC123_readonly");
        result.ViewRoleExists.ShouldBeTrue();
        result.ReadOnlyRoleExists.ShouldBeTrue();
        result.MainRoleExists.ShouldBeFalse();
    }

    [Fact]
    public async Task GetAsync_NoLicencePlateAndNoSavedRole_HasNoViewRole()
    {
        var tenant = CreateTenant("acme");
        _tenantRepository.GetAsync(tenant.Id).Returns(tenant);
        _settingManager.GetOrNullAsync(SettingName, "T", tenant.Id.ToString()).Returns((string?)null);

        var result = await _service.GetAsync(tenant.Id);

        result.LicencePlate.ShouldBeNull();
        result.ExpectedReadOnlyRole.ShouldBeNull();
        result.ViewRole.ShouldBeNull();
        result.ViewRoleExists.ShouldBeFalse();
        // Nothing to look up, so the repository should never be queried for this tenant.
        await _reportColumnsMapRepository.DidNotReceiveWithAnyArgs().RoleExistsAsync(default!);
    }

    [Fact]
    public async Task GetAsync_SavedRoleExists_TakesPrecedenceOverLicencePlateRole()
    {
        var tenant = CreateTenant("acme");
        tenant.ExtraProperties["LicencePlate"] = "T_ABC123";
        _tenantRepository.GetAsync(tenant.Id).Returns(tenant);
        _settingManager.GetOrNullAsync(SettingName, "T", tenant.Id.ToString()).Returns("legacy_custom_role");
        _reportColumnsMapRepository.RoleExistsAsync(Arg.Any<string>()).Returns(true);

        var result = await _service.GetAsync(tenant.Id);

        result.ViewRole.ShouldBe("legacy_custom_role");
        result.ViewRoleExists.ShouldBeTrue();
        // The license-plate role info is still surfaced alongside the saved (legacy) role.
        result.LicencePlate.ShouldBe("T_ABC123");
        result.ExpectedReadOnlyRole.ShouldBe("T_ABC123_readonly");
    }

    [Fact]
    public async Task GetAsync_LegacyTenantWithSavedRole_ChecksSavedRoleExists()
    {
        var tenant = CreateTenant("acme");
        _tenantRepository.GetAsync(tenant.Id).Returns(tenant);
        _settingManager.GetOrNullAsync(SettingName, "T", tenant.Id.ToString()).Returns("acme_readonly");
        _reportColumnsMapRepository.RoleExistsAsync("acme_readonly").Returns(false);

        var result = await _service.GetAsync(tenant.Id);

        result.ViewRole.ShouldBe("acme_readonly");
        result.ViewRoleExists.ShouldBeFalse();
        result.LicencePlate.ShouldBeNull();
    }

    [Fact]
    public async Task AssignRoleToViewsAsync_NoLicencePlateAndNoSavedRole_ThrowsUserFriendlyException()
    {
        var tenant = CreateTenant("acme");
        _tenantRepository.GetAsync(tenant.Id).Returns(tenant);
        _settingManager.GetOrNullAsync(SettingName, "T", tenant.Id.ToString()).Returns((string?)null);

        await Should.ThrowAsync<UserFriendlyException>(() => _service.AssignRoleToViewsAsync(tenant.Id));

        await _backgroundJobManager.DidNotReceiveWithAnyArgs().EnqueueAsync<object>(default!);
    }

    [Fact]
    public async Task AssignRoleToViewsAsync_RoleDoesNotExist_ThrowsUserFriendlyException()
    {
        var tenantId = Guid.NewGuid();
        _settingManager.GetOrNullAsync(SettingName, "T", tenantId.ToString()).Returns("ghost_role");
        _reportColumnsMapRepository.RoleExistsAsync("ghost_role").Returns(false);

        await Should.ThrowAsync<UserFriendlyException>(() => _service.AssignRoleToViewsAsync(tenantId));

        await _backgroundJobManager.DidNotReceiveWithAnyArgs().EnqueueAsync<object>(default!);
    }

    [Fact]
    public async Task AssignRoleToViewsAsync_SavedRoleExists_EnqueuesJob()
    {
        var tenantId = Guid.NewGuid();
        _settingManager.GetOrNullAsync(SettingName, "T", tenantId.ToString()).Returns("acme_readonly");
        _reportColumnsMapRepository.RoleExistsAsync("acme_readonly").Returns(true);

        await _service.AssignRoleToViewsAsync(tenantId);

        await _backgroundJobManager.Received(1).EnqueueAsync(
            Arg.Is<Unity.Reporting.BackgroundJobs.AssignViewRoleBackgroundJobArgs>(a => a.TenantId == tenantId),
            Arg.Any<BackgroundJobPriority>(), Arg.Any<TimeSpan?>());
    }

    [Fact]
    public async Task AssignRoleToViewsAsync_NoSavedRole_FallsBackToLicencePlateReadOnlyRole()
    {
        var tenant = CreateTenant("acme");
        tenant.ExtraProperties["LicencePlate"] = "T_ABC123";
        _tenantRepository.GetAsync(tenant.Id).Returns(tenant);
        _settingManager.GetOrNullAsync(SettingName, "T", tenant.Id.ToString()).Returns((string?)null);
        _reportColumnsMapRepository.RoleExistsAsync("T_ABC123_readonly").Returns(true);

        await _service.AssignRoleToViewsAsync(tenant.Id);

        await _reportColumnsMapRepository.Received(1).RoleExistsAsync("T_ABC123_readonly");
        await _backgroundJobManager.Received(1).EnqueueAsync(
            Arg.Is<Unity.Reporting.BackgroundJobs.AssignViewRoleBackgroundJobArgs>(a => a.TenantId == tenant.Id),
            Arg.Any<BackgroundJobPriority>(), Arg.Any<TimeSpan?>());
    }

    private static ReportColumnsMap CreateMap(string viewName, ViewStatus status) =>
        new() { ViewName = viewName, ViewStatus = status, CorrelationProvider = "worksheet" };

    [Fact]
    public async Task GetTenantDatabaseInfoAsync_ReconcilesViewsAndMappings_ProblemsFirst()
    {
        var tenant = CreateTenant("acme");
        tenant.ExtraProperties["LicencePlate"] = "T_ABC123";
        _tenantRepository.GetAsync(tenant.Id).Returns(tenant);
        _settingManager.GetOrNullAsync(SettingName, "T", tenant.Id.ToString()).Returns((string?)null);
        _reportColumnsMapRepository.RoleExistsAsync("T_ABC123_readonly").Returns(true);
        _reportColumnsMapRepository.GetReportingViewGrantsAsync("T_ABC123_readonly").Returns(
        [
            new ReportingViewGrant("ok_view", true),
            new ReportingViewGrant("ungranted_view", false),
            new ReportingViewGrant("orphan_view", true),
            new ReportingViewGrant("failed_view", true)
        ]);
        _reportColumnsMapRepository.GetListAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(
        [
            CreateMap("ok_view", ViewStatus.SUCCESS),
            CreateMap("UNGRANTED_VIEW", ViewStatus.SUCCESS),
            CreateMap("failed_view", ViewStatus.FAILED),
            CreateMap("missing_view", ViewStatus.SUCCESS),
            CreateMap("stuck_view", ViewStatus.GENERATING),
            CreateMap("", ViewStatus.GENERATING)
        ]);

        var result = await _service.GetTenantDatabaseInfoAsync(tenant.Id);

        result.Error.ShouldBeNull();
        result.Role.ShouldNotBeNull();
        result.Role.ViewRole.ShouldBe("T_ABC123_readonly");
        result.Role.ViewRoleExists.ShouldBeTrue();
        result.Views.Select(v => (v.ViewName, v.Issue)).ShouldBe(
        [
            ("failed_view", ReportingViewIssue.GenerationFailed),
            ("missing_view", ReportingViewIssue.Missing),
            ("orphan_view", ReportingViewIssue.Unmapped),
            ("stuck_view", ReportingViewIssue.Generating),
            ("ungranted_view", ReportingViewIssue.NotGranted),
            ("ok_view", ReportingViewIssue.None)
        ]);
        result.Views.Single(v => v.ViewName == "missing_view").InDatabase.ShouldBeFalse();
        result.Views.Single(v => v.ViewName == "missing_view").Granted.ShouldBeNull();
    }

    [Fact]
    public async Task GetTenantDatabaseInfoAsync_ViewRoleMissing_DoesNotCheckGrants()
    {
        var tenant = CreateTenant("acme");
        var tenantId = tenant.Id;
        _tenantRepository.GetAsync(tenantId).Returns(tenant);
        _settingManager.GetOrNullAsync(SettingName, "T", tenantId.ToString()).Returns("ghost_role");
        _reportColumnsMapRepository.RoleExistsAsync("ghost_role").Returns(false);
        _reportColumnsMapRepository.GetReportingViewGrantsAsync(null).Returns([new ReportingViewGrant("ok_view", false)]);
        _reportColumnsMapRepository.GetListAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns([CreateMap("ok_view", ViewStatus.SUCCESS)]);

        var result = await _service.GetTenantDatabaseInfoAsync(tenantId);

        result.Role!.ViewRoleExists.ShouldBeFalse();
        result.ViewRoleCanLogin.ShouldBeFalse();
        await _reportColumnsMapRepository.DidNotReceiveWithAnyArgs().GetRoleAccessAsync(default!);
        // has_table_privilege errors on a non-existent role, so it must never be passed through.
        await _reportColumnsMapRepository.DidNotReceive().GetReportingViewGrantsAsync("ghost_role");
        var view = result.Views.ShouldHaveSingleItem();
        view.Granted.ShouldBeNull();
        view.Issue.ShouldBe(ReportingViewIssue.None);
    }

    [Fact]
    public async Task GetTenantDatabaseInfoAsync_DatabaseError_ReturnsError()
    {
        var tenant = CreateTenant("acme");
        var tenantId = tenant.Id;
        _tenantRepository.GetAsync(tenantId).Returns(tenant);
        _settingManager.GetOrNullAsync(SettingName, "T", tenantId.ToString()).Returns("acme_readonly");
        _reportColumnsMapRepository.RoleExistsAsync("acme_readonly").Returns(true);
        _reportColumnsMapRepository.GetReportingViewGrantsAsync(Arg.Any<string?>())
            .Returns<List<ReportingViewGrant>>(_ => throw new InvalidOperationException("boom"));

        var result = await _service.GetTenantDatabaseInfoAsync(tenantId);

        result.Error.ShouldNotBeNull();
        result.Views.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetTenantDatabaseInfoAsync_ViewRoleExists_ReportsLoginAndSchemaAccess()
    {
        var tenant = CreateTenant("acme");
        tenant.ExtraProperties["LicencePlate"] = "T_ABC123";
        _tenantRepository.GetAsync(tenant.Id).Returns(tenant);
        _settingManager.GetOrNullAsync(SettingName, "T", tenant.Id.ToString()).Returns("legacy_role");
        _reportColumnsMapRepository.RoleExistsAsync("legacy_role").Returns(true);
        _reportColumnsMapRepository.RoleExistsAsync("T_ABC123").Returns(true);
        _reportColumnsMapRepository.RoleExistsAsync("T_ABC123_readonly").Returns(false);
        _reportColumnsMapRepository.GetRoleAccessAsync("legacy_role").Returns(new ReportingRoleAccess(true, false));
        _reportColumnsMapRepository.GetReportingViewGrantsAsync("legacy_role").Returns([]);
        _reportColumnsMapRepository.GetListAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns([]);

        var result = await _service.GetTenantDatabaseInfoAsync(tenant.Id);

        result.ViewRoleCanLogin.ShouldBeTrue();
        result.ViewRoleHasSchemaUsage.ShouldBeFalse();
        result.Role!.ViewRole.ShouldBe("legacy_role");
        result.Role.ExpectedReadOnlyRole.ShouldBe("T_ABC123_readonly");
        result.Role.MainRoleExists.ShouldBeTrue();
        result.Role.ReadOnlyRoleExists.ShouldBeFalse();
    }
}
