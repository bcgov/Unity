using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Unity.GrantManager.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore.GlobalFilters;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Modularity;
using Volo.Abp.PermissionManagement;
using Volo.Abp.Uow;

namespace Unity.GrantManager;

[DependsOn(
    typeof(GrantManagerEntityFrameworkCoreModule),
    typeof(GrantManagerTestBaseModule)
)]
public class WebTestInMemoryDbModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<FeatureManagementOptions>(options =>
        {
            options.SaveStaticFeaturesToDatabase = false;
            options.IsDynamicFeatureStoreEnabled = false;
        });

        Configure<PermissionManagementOptions>(options =>
        {
            options.SaveStaticPermissionsToDatabase = false;
            options.IsDynamicPermissionStoreEnabled = false;
        });

        Configure<AbpEfCoreGlobalFilterOptions>(options =>
        {
            options.UseDbFunction = false;
        });

        context.Services.AddAlwaysDisableUnitOfWorkTransaction();

        var inMemoryDatabaseName = $"WebTests_{System.Guid.NewGuid():N}";

        Configure<AbpDbContextOptions>(options =>
        {
            options.Configure(abpDbContextConfigurationContext =>
            {
                abpDbContextConfigurationContext.DbContextOptions
                    .UseInMemoryDatabase(inMemoryDatabaseName);
            });
        });
    }
}
