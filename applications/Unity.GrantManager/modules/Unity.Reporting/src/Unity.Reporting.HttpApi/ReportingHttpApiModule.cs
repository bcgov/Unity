using Localization.Resources.AbpUi;
using Unity.Reporting.Localization;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Localization;
using Volo.Abp.Modularity;
using Microsoft.Extensions.DependencyInjection;

namespace Unity.Reporting;

[DependsOn(
    typeof(ReportingApplicationContractsModule),
    typeof(AbpAspNetCoreMvcModule))]
public class ReportingHttpApiModule : AbpModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        PreConfigure<IMvcBuilder>(mvcBuilder =>
        {
            mvcBuilder.AddApplicationPartIfNotExists(typeof(ReportingHttpApiModule).Assembly);
        });
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpLocalizationOptions>(options =>
        {
            options.Resources
                .Get<ReportingResource>()
                .AddBaseTypes(typeof(AbpUiResource));
        });
    }
}
