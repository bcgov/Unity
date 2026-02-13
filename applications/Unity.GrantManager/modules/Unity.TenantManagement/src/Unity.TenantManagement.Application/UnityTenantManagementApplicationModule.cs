using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;
using Volo.Abp.TenantManagement;

namespace Unity.TenantManagement
{
    [DependsOn(
        typeof(AbpTenantManagementDomainModule),
        typeof(UnityTenantManagementApplicationContractsModule),
        typeof(AbpTenantManagementApplicationModule),
        typeof(AbpAutoMapperModule)
    )]
    public class UnityTenantManagementApplicationModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddAutoMapperObjectMapper<UnityTenantManagementApplicationModule>();
            
            Configure<AbpAutoMapperOptions>(options =>
            {
                options.AddMaps<UnityTenantManagementApplicationModule>(validate: true);
            });
        }
    }
}
