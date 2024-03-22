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

namespace Unity.GrantManager;

[DependsOn(
    typeof(GrantManagerApplicationContractsModule),
    typeof(AbpIdentityHttpApiModule),
    typeof(AbpPermissionManagementHttpApiModule),
    typeof(UnityTenantManagementHttpApiModule),
    typeof(AbpFeatureManagementHttpApiModule),
    typeof(AbpSettingManagementHttpApiModule)
    )]
[DependsOn(typeof(NotificationsHttpApiModule))]
    public class GrantManagerHttpApiModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ConfigureLocalization();
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
}
