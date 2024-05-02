using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;
using Volo.Abp.Application;
using Volo.Abp.Localization.ExceptionHandling;
using Unity.Payments.Localization;
using Volo.Abp.VirtualFileSystem;
using Volo.Abp.Localization;
using Volo.Abp.Validation.Localization;
using Localization.Resources.AbpUi;
using Volo.Abp.AspNetCore.Mvc;
using Unity.Payments.EntityFrameworkCore;
using Volo.Abp.MultiTenancy;

namespace Unity.Payments;

[DependsOn(
    typeof(AbpVirtualFileSystemModule),    
    typeof(AbpDddApplicationModule),
    typeof(AbpAutoMapperModule),
    typeof(AbpVirtualFileSystemModule),
    typeof(PaymentsApplicationContractsModule)
    )]
public class PaymentsApplicationModule : AbpModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        PreConfigure<IMvcBuilder>(mvcBuilder =>
        {
            mvcBuilder.AddApplicationPartIfNotExists(typeof(PaymentsApplicationModule).Assembly);
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
            options.FileSets.AddEmbedded<PaymentsApplicationModule>();
        });
       
        context.Services.AddAutoMapperObjectMapper<PaymentsApplicationModule>();
        Configure<AbpAutoMapperOptions>(options =>
        {
            options.AddMaps<PaymentsApplicationModule>(validate: true);
        });

        context.Services.AddHttpClientProxies(
           typeof(PaymentsApplicationContractsModule).Assembly,
           PaymentsRemoteServiceConsts.RemoteServiceName
        );

        Configure<AbpAspNetCoreMvcOptions>(options =>
        {
            options.ConventionalControllers.Create(typeof(PaymentsApplicationModule).Assembly);
        });

        context.Services.AddAssemblyOf<PaymentsApplicationModule>();

        context.Services.AddAbpDbContext<PaymentsDbContext>(options =>
        {
            /* Add custom repositories here. */
        });
    }
}
