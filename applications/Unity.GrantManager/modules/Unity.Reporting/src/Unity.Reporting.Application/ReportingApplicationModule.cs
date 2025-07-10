using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;
using Volo.Abp.Application;
using Volo.Abp.MultiTenancy;
using Volo.Abp.VirtualFileSystem;
using Unity.Reporting.EntityFrameworkCore;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.Reporting;

[DependsOn(
    typeof(ReportingApplicationContractsModule),
    typeof(AbpDddApplicationModule),
    typeof(AbpAutoMapperModule)    
    )]
public class ReportingApplicationModule : AbpModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        PreConfigure<IMvcBuilder>(mvcBuilder =>
        {
            mvcBuilder.AddApplicationPartIfNotExists(typeof(ReportingApplicationModule).Assembly);
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
            options.FileSets.AddEmbedded<ReportingApplicationModule>();
        });

        context.Services.AddAutoMapperObjectMapper<ReportingApplicationModule>();
        Configure<AbpAutoMapperOptions>(options =>
        {
            options.AddMaps<ReportingApplicationModule>(validate: true);
        });

        Configure<AbpAspNetCoreMvcOptions>(options =>
        {
            options.ConventionalControllers.Create(typeof(ReportingApplicationModule).Assembly);
        });

        context.Services.AddAssemblyOf<ReportingApplicationModule>();

        context.Services.AddAbpDbContext<ReportingDbContext>(options =>
        {
            /* Add custom repositories here. */
        });
    }
}
