using Microsoft.Extensions.DependencyInjection;
using Unity.TenantManagement.EntityFrameworkCore;
using Volo.Abp.Modularity;

namespace Unity.TenantManagement;

[DependsOn(
    typeof(UnityTenantManagementApplicationModule),
    typeof(UnityTenantManagementEntityFrameworkCoreTestModule))]
public class AbpTenantManagementApplicationTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddAlwaysAllowAuthorization();
    }
}
