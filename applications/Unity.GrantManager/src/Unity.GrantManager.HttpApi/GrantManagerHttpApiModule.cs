using Localization.Resources.AbpUi;
using Unity.GrantManager.Localization;
using Unity.TenantManagement;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Identity;
using Volo.Abp.Localization;
using Volo.Abp.Modularity;
using Volo.Abp.PermissionManagement.HttpApi;
using Volo.Abp.SettingManagement;
using Unity.Notifications;
using Unity.GrantManager.Controllers.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace Unity.GrantManager;

[DependsOn(
    typeof(GrantManagerApplicationContractsModule),
    typeof(AbpIdentityHttpApiModule),
    typeof(AbpPermissionManagementHttpApiModule),
    typeof(UnityTenantManagementHttpApiModule),
    typeof(AbpFeatureManagementHttpApiModule),
    typeof(AbpSettingManagementHttpApiModule),
    typeof(NotificationsHttpApiModule)
    )]

public class GrantManagerHttpApiModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var services = context.Services;

        ConfigureLocalization();
        ConfigureFilters(services);
    }

    private void ConfigureLocalization()
    {
        Configure<AbpLocalizationOptions>(options =>
        {
            options.Resources
                .Get<GrantManagerResource>()
                .AddBaseTypes(
                    typeof(AbpUiResource)
                );
        });
    }

    private static void ConfigureFilters(IServiceCollection services)
    {
        services.AddScoped<ApiKeyAuthorizationFilter>();
    }
}
