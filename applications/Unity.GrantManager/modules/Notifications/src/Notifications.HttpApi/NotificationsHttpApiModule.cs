using Localization.Resources.AbpUi;
using Notifications.Localization;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Localization;
using Volo.Abp.Modularity;
using Microsoft.Extensions.DependencyInjection;

namespace Notifications;

[DependsOn(
    typeof(NotificationsApplicationContractsModule),
    typeof(AbpAspNetCoreMvcModule))]
public class NotificationsHttpApiModule : AbpModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        PreConfigure<IMvcBuilder>(mvcBuilder =>
        {
            mvcBuilder.AddApplicationPartIfNotExists(typeof(NotificationsHttpApiModule).Assembly);
        });
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpLocalizationOptions>(options =>
        {
            options.Resources
                .Get<NotificationsResource>()
                .AddBaseTypes(typeof(AbpUiResource));
        });
    }
}
