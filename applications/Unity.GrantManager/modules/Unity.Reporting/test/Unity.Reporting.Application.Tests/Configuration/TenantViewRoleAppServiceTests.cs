using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using System;
using System.Reflection;
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
            _tenantRepository, _settingManager, _backgroundJobManager, _reportColumnsMapRepository, _currentTenant);

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
    public async Task GetAsync_TenantHasLicencePlate_NoSavedRole_DefaultsToLicencePlateReadOnlyRole()
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
        result.IsDefaultInferred.ShouldBeTrue();
        result.ReadOnlyRoleExists.ShouldBeTrue();
        result.MainRoleExists.ShouldBeFalse();
    }

    [Fact]
    public async Task GetAsync_TenantHasNoLicencePlate_FallsBackToLegacyTenantNamePattern()
    {
        var tenant = CreateTenant("acme");
        _tenantRepository.GetAsync(tenant.Id).Returns(tenant);
        _settingManager.GetOrNullAsync(SettingName, "T", tenant.Id.ToString()).Returns((string?)null);

        var result = await _service.GetAsync(tenant.Id);

        result.LicencePlate.ShouldBeNull();
        result.ExpectedReadOnlyRole.ShouldBeNull();
        result.ViewRole.ShouldBe("acme_readonly");
        result.IsDefaultInferred.ShouldBeTrue();
        result.ReadOnlyRoleExists.ShouldBeFalse();
        result.MainRoleExists.ShouldBeFalse();
        // No license plate - existence isn't checked against anything, so the repository should
        // never be queried for this tenant.
        await _reportColumnsMapRepository.DidNotReceiveWithAnyArgs().RoleExistsAsync(default!);
    }

    [Fact]
    public async Task GetAsync_SavedRoleExists_TakesPrecedenceOverLicencePlateDefault()
    {
        var tenant = CreateTenant("acme");
        tenant.ExtraProperties["LicencePlate"] = "T_ABC123";
        _tenantRepository.GetAsync(tenant.Id).Returns(tenant);
        _settingManager.GetOrNullAsync(SettingName, "T", tenant.Id.ToString()).Returns("legacy_custom_role");
        _reportColumnsMapRepository.RoleExistsAsync(Arg.Any<string>()).Returns(true);

        var result = await _service.GetAsync(tenant.Id);

        result.ViewRole.ShouldBe("legacy_custom_role");
        result.IsDefaultInferred.ShouldBeFalse();
        // The license-plate role info is still surfaced alongside the saved (legacy) role.
        result.LicencePlate.ShouldBe("T_ABC123");
        result.ExpectedReadOnlyRole.ShouldBe("T_ABC123_readonly");
    }

    [Fact]
    public async Task UpdateAsync_RoleDoesNotExist_ThrowsUserFriendlyException()
    {
        var tenant = CreateTenant("acme");
        _tenantRepository.GetAsync(tenant.Id).Returns(tenant);
        _reportColumnsMapRepository.RoleExistsAsync("nonexistent_role").Returns(false);

        await Should.ThrowAsync<UserFriendlyException>(
            () => _service.UpdateAsync(tenant.Id, new UpdateTenantViewRoleDto { ViewRole = "nonexistent_role" }));

        await _settingManager.DidNotReceive().SetAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>());
    }

    [Fact]
    public async Task UpdateAsync_RoleExists_PersistsSetting()
    {
        var tenant = CreateTenant("acme");
        _tenantRepository.GetAsync(tenant.Id).Returns(tenant);
        _reportColumnsMapRepository.RoleExistsAsync("acme_readonly").Returns(true);

        var result = await _service.UpdateAsync(tenant.Id, new UpdateTenantViewRoleDto { ViewRole = "acme_readonly" });

        result.ViewRole.ShouldBe("acme_readonly");
        result.IsDefaultInferred.ShouldBeFalse();
        await _settingManager.Received(1).SetAsync(
            SettingName, "acme_readonly", "T", tenant.Id.ToString(), Arg.Any<bool>());
    }

    [Fact]
    public async Task AssignRoleToViewsAsync_NoRoleConfigured_ThrowsUserFriendlyException()
    {
        var tenantId = Guid.NewGuid();
        _settingManager.GetOrNullAsync(SettingName, "T", tenantId.ToString()).Returns((string?)null);

        await Should.ThrowAsync<UserFriendlyException>(() => _service.AssignRoleToViewsAsync(tenantId));

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
    public async Task AssignRoleToViewsAsync_RoleExists_EnqueuesJob()
    {
        var tenantId = Guid.NewGuid();
        _settingManager.GetOrNullAsync(SettingName, "T", tenantId.ToString()).Returns("acme_readonly");
        _reportColumnsMapRepository.RoleExistsAsync("acme_readonly").Returns(true);

        await _service.AssignRoleToViewsAsync(tenantId);

        await _backgroundJobManager.Received(1).EnqueueAsync(
            Arg.Is<Unity.Reporting.BackgroundJobs.AssignViewRoleBackgroundJobArgs>(a => a.TenantId == tenantId),
            Arg.Any<BackgroundJobPriority>(), Arg.Any<TimeSpan?>());
    }
}
