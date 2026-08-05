using Microsoft.Extensions.DependencyInjection;
using Unity.Flex;
using Unity.GrantManager;
using Volo.Abp.Application;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Mapperly;
using Volo.Abp.Modularity;
using Volo.Abp.MultiTenancy;
using Volo.Abp.TenantManagement;
using Volo.Abp.VirtualFileSystem;

namespace Unity.AI;

[DependsOn(
    typeof(AIApplicationContractsModule),
    typeof(AbpDddApplicationModule),
    typeof(AbpMapperlyModule),
    typeof(AbpTenantManagementDomainModule),
    typeof(FlexApplicationModule),
    typeof(GrantManagerDomainModule)
    )]
public class AIApplicationModule : AbpModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        PreConfigure<IMvcBuilder>(mvcBuilder =>
        {
            mvcBuilder.AddApplicationPartIfNotExists(typeof(AIApplicationModule).Assembly);
        });
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddMemoryCache();

        Configure<AbpMultiTenancyOptions>(options =>
        {
            options.IsEnabled = true;
        });

        Configure<AbpVirtualFileSystemOptions>(options =>
        {
            options.FileSets.AddEmbedded<AIApplicationModule>();
        });

        context.Services.AddMapperlyObjectMapper<AIApplicationModule>();

        context.Services.AddHttpClientProxies(
            typeof(AIApplicationContractsModule).Assembly,
            AIRemoteServiceConsts.RemoteServiceName
        );

        Configure<AbpAspNetCoreMvcOptions>(options =>
        {
            options.ConventionalControllers.Create(typeof(AIApplicationModule).Assembly);
        });

        context.Services.AddAssemblyOf<AIApplicationModule>();
    }
}
