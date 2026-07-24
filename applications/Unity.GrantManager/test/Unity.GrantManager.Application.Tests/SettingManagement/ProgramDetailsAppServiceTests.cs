using System;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
using Volo.Abp.Authorization;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Entities;
using Volo.Abp.MultiTenancy;
using Volo.Abp.TenantManagement;
using Xunit;
using Xunit.Abstractions;

namespace Unity.GrantManager.SettingManagement;

public class ProgramDetailsAppServiceTests(ITestOutputHelper outputHelper) : GrantManagerApplicationTestBase(outputHelper)
{
    [Fact]
    public async Task GetProgramDetailsAsync_WithinTenantContext_ShouldReturnMappedExtraProperties()
    {
        var tenantId = Guid.NewGuid();
        var tenant = CreateTenant(tenantId);
        tenant.SetProperty("DisplayName", "My Program");
        tenant.SetProperty("Division", "Housing Division");
        tenant.SetProperty("Branch", "Grants Branch");
        tenant.SetProperty("Description", "Sample description");

        var tenantRepository = Substitute.For<ITenantRepository>();
        tenantRepository.FindAsync(tenantId).Returns(tenant);

        var appService = CreateAppService(tenantRepository);
        var currentTenant = GetRequiredService<ICurrentTenant>();

        ProgramDetailsDto result;
        using (currentTenant.Change(tenantId))
        {
            result = await appService.GetProgramDetailsAsync();
        }

        result.DisplayName.ShouldBe("My Program");
        result.Division.ShouldBe("Housing Division");
        result.Branch.ShouldBe("Grants Branch");
        result.Description.ShouldBe("Sample description");
    }

    [Fact]
    public async Task GetProgramDetailsAsync_WithMissingExtraProperties_ShouldReturnEmptyStrings()
    {
        var tenantId = Guid.NewGuid();
        var tenant = CreateTenant(tenantId);

        var tenantRepository = Substitute.For<ITenantRepository>();
        tenantRepository.FindAsync(tenantId).Returns(tenant);

        var appService = CreateAppService(tenantRepository);
        var currentTenant = GetRequiredService<ICurrentTenant>();

        ProgramDetailsDto result;
        using (currentTenant.Change(tenantId))
        {
            result = await appService.GetProgramDetailsAsync();
        }

        result.DisplayName.ShouldBe(string.Empty);
        result.Division.ShouldBe(string.Empty);
        result.Branch.ShouldBe(string.Empty);
        result.Description.ShouldBe(string.Empty);
    }

    [Fact]
    public async Task GetProgramDetailsAsync_WithoutTenantContext_ShouldThrowAbpAuthorizationException()
    {
        var tenantRepository = Substitute.For<ITenantRepository>();
        var appService = CreateAppService(tenantRepository);
        var currentTenant = GetRequiredService<ICurrentTenant>();

        using (currentTenant.Change(null))
        {
            await Should.ThrowAsync<AbpAuthorizationException>(() => appService.GetProgramDetailsAsync());
        }
    }

    [Fact]
    public async Task GetProgramDetailsAsync_WithUnknownTenant_ShouldThrowEntityNotFoundException()
    {
        var tenantId = Guid.NewGuid();
        var tenantRepository = Substitute.For<ITenantRepository>();
        tenantRepository.FindAsync(tenantId).Returns((Tenant?)null);

        var appService = CreateAppService(tenantRepository);
        var currentTenant = GetRequiredService<ICurrentTenant>();

        using (currentTenant.Change(tenantId))
        {
            await Should.ThrowAsync<EntityNotFoundException>(() => appService.GetProgramDetailsAsync());
        }
    }

    [Fact]
    public async Task UpdateProgramDetailsAsync_ShouldNormalizeAndPersistTrimmedValues()
    {
        var tenantId = Guid.NewGuid();
        var tenant = CreateTenant(tenantId);

        var tenantRepository = Substitute.For<ITenantRepository>();
        tenantRepository.FindAsync(tenantId).Returns(tenant);

        var appService = CreateAppService(tenantRepository);
        var currentTenant = GetRequiredService<ICurrentTenant>();

        var input = new UpdateProgramDetailsDto
        {
            DisplayName = "  My Program  ",
            Division = "  Housing Division  ",
            Branch = null,
            Description = "   "
        };

        using (currentTenant.Change(tenantId))
        {
            await appService.UpdateProgramDetailsAsync(input);
        }

        tenant.GetProperty("DisplayName")?.ToString().ShouldBe("My Program");
        tenant.GetProperty("Division")?.ToString().ShouldBe("Housing Division");
        tenant.GetProperty("Branch")?.ToString().ShouldBe(string.Empty);
        tenant.GetProperty("Description")?.ToString().ShouldBe(string.Empty);
        await tenantRepository.Received(1).UpdateAsync(tenant);
    }

    [Fact]
    public async Task UpdateProgramDetailsAsync_WithoutTenantContext_ShouldThrowAbpAuthorizationException()
    {
        var tenantRepository = Substitute.For<ITenantRepository>();
        var appService = CreateAppService(tenantRepository);
        var currentTenant = GetRequiredService<ICurrentTenant>();

        var input = new UpdateProgramDetailsDto { DisplayName = "Test" };

        using (currentTenant.Change(null))
        {
            await Should.ThrowAsync<AbpAuthorizationException>(() => appService.UpdateProgramDetailsAsync(input));
        }
    }

    private ProgramDetailsAppService CreateAppService(ITenantRepository tenantRepository)
    {
        var appService = new ProgramDetailsAppService(tenantRepository)
        {
            LazyServiceProvider = GetRequiredService<IAbpLazyServiceProvider>()
        };
        return appService;
    }

    private static Tenant CreateTenant(Guid id)
    {
        var tenant = (Tenant)Activator.CreateInstance(typeof(Tenant), nonPublic: true)!;
        EntityHelper.TrySetId(tenant, () => id);
        return tenant;
    }
}
