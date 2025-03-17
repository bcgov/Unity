using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;
using Volo.Abp.Application;

namespace Unity.Reporting;

[DependsOn(
    typeof(ReportingApplicationContractsModule),
    typeof(AbpDddApplicationModule),
    typeof(AbpAutoMapperModule)
    )]
public class ReportingApplicationModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddAutoMapperObjectMapper<ReportingApplicationModule>();
        Configure<AbpAutoMapperOptions>(options =>
        {
            options.AddMaps<ReportingApplicationModule>(validate: true);
        });
    }
}
