using Microsoft.Extensions.DependencyInjection;
using Unity.Flex;
using Volo.Abp.Mapperly;
using Volo.Abp.Modularity;
using Volo.Abp.TenantManagement;

namespace Unity.TenantManagement
{
    [DependsOn(
        typeof(AbpTenantManagementDomainModule),
        typeof(UnityTenantManagementApplicationContractsModule),
        typeof(AbpTenantManagementApplicationModule),
        typeof(AbpMapperlyModule),
        typeof(FlexApplicationContractsModule)
    )]
    public class UnityTenantManagementApplicationModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddMapperlyObjectMapper<UnityTenantManagementApplicationModule>();
        }
    }
}
