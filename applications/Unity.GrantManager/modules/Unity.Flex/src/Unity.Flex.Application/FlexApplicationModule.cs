using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AutoMapper;
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
    typeof(AbpAutoMapperModule),
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

        context.Services.AddAutoMapperObjectMapper<FlexApplicationModule>();
        Configure<AbpAutoMapperOptions>(options =>
        {
            options.AddMaps<FlexApplicationModule>(validate: true);
        });

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
