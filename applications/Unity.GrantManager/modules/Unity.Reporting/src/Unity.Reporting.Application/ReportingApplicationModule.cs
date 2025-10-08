using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;
using Volo.Abp.Application;
using Volo.Abp.MultiTenancy;
using Volo.Abp.VirtualFileSystem;
using Unity.Reporting.EntityFrameworkCore;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.SettingManagement;

namespace Unity.Reporting;

[DependsOn(
    typeof(ReportingApplicationContractsModule),
    typeof(ReportingEntityFrameworkCoreModule),
    typeof(AbpDddApplicationModule),
    typeof(AbpAutoMapperModule),
    typeof(AbpSettingManagementApplicationModule)
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

        context.Services.AddHttpClientProxies(
            typeof(ReportingApplicationContractsModule).Assembly,
            ReportingRemoteServiceConsts.RemoteServiceName
        );

        Configure<AbpAspNetCoreMvcOptions>(options =>
        {
            options.ConventionalControllers.Create(typeof(ReportingApplicationModule).Assembly);
        });

        context.Services.AddAssemblyOf<ReportingApplicationModule>();
    }
}
