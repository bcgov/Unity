using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Mapperly;
using Volo.Abp.Modularity;
using Volo.Abp.TenantManagement;

namespace Unity.TenantManagement
{
    [DependsOn(
        typeof(AbpTenantManagementDomainModule),
        typeof(UnityTenantManagementApplicationContractsModule),
        typeof(AbpTenantManagementApplicationModule),
        typeof(AbpMapperlyModule)
    )]
    public class UnityTenantManagementApplicationModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddMapperlyObjectMapper<UnityTenantManagementApplicationModule>();
        }
    }
}
