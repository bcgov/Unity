using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Mapperly;
using Volo.Abp.Modularity;
using Volo.Abp.Application;
using Volo.Abp.Domain;
using Volo.Abp.Validation;
using Volo.Abp.VirtualFileSystem;
using Unity.Flex.EntityFrameworkCore;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.MultiTenancy;

namespace Unity.Flex;

[DependsOn(    
    typeof(AbpDddApplicationModule),
    typeof(AbpMapperlyModule),
    typeof(AbpValidationModule),
    typeof(AbpDddDomainSharedModule),
    typeof(AbpVirtualFileSystemModule),
    typeof(FlexApplicationContractsModule)
    )]
public class FlexApplicationModule : AbpModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        PreConfigure<IMvcBuilder>(mvcBuilder =>
        {
            mvcBuilder.AddApplicationPartIfNotExists(typeof(FlexApplicationModule).Assembly);
        });
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpMultiTenancyOptions>(options =>
        {
            options.IsEnabled = true;
        });

        Configure<AbpVirtualFileSystemOptions>(options =>
        {
            options.FileSets.AddEmbedded<FlexApplicationModule>();
        });       

        context.Services.AddMapperlyObjectMapper<FlexApplicationModule>();

        context.Services.AddHttpClientProxies(
           typeof(FlexApplicationContractsModule).Assembly,
           FlexRemoteServiceConsts.RemoteServiceName
        );

        Configure<AbpAspNetCoreMvcOptions>(options =>
        {
            options.ConventionalControllers.Create(typeof(FlexApplicationModule).Assembly);
        });

        context.Services.AddAssemblyOf<FlexApplicationModule>();

        context.Services.AddAbpDbContext<FlexDbContext>(options =>
        {
            /* Add custom repositories here. */
        });
    }
}
