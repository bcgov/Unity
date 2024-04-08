using Localization.Resources.AbpUi;
using Unity.Payments.Localization;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Localization;
using Volo.Abp.Modularity;
using Microsoft.Extensions.DependencyInjection;

namespace Unity.Payments;

[DependsOn(
    typeof(PaymentsApplicationContractsModule),
    typeof(AbpAspNetCoreMvcModule))]
public class PaymentsHttpApiModule : AbpModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        PreConfigure<IMvcBuilder>(mvcBuilder =>
        {
            mvcBuilder.AddApplicationPartIfNotExists(typeof(PaymentsHttpApiModule).Assembly);
        });
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpLocalizationOptions>(options =>
        {
            options.Resources
                .Get<PaymentsResource>()
                .AddBaseTypes(typeof(AbpUiResource));
        });
    }
}
