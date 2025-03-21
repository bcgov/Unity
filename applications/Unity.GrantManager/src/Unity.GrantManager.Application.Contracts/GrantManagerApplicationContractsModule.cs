using Unity.TenantManagement;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Volo.Abp.ObjectExtending;
using Volo.Abp.PermissionManagement;
using Volo.Abp.SettingManagement;
using Unity.Notifications;
using Unity.Reporting;

namespace Unity.GrantManager;

[DependsOn(
    typeof(GrantManagerDomainSharedModule),
    typeof(AbpFeatureManagementApplicationContractsModule),
    typeof(AbpIdentityApplicationContractsModule),
    typeof(AbpPermissionManagementApplicationContractsModule),
    typeof(AbpSettingManagementApplicationContractsModule),
    typeof(UnityTenantManagementApplicationContractsModule),
    typeof(AbpObjectExtendingModule),
    typeof(NotificationsApplicationContractsModule)    
)]
[DependsOn(typeof(ReportingApplicationContractsModule))]
    public class GrantManagerApplicationContractsModule : AbpModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        GrantManagerDtoExtensions.Configure();
    }
}
