using Unity.TenantManagement;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Volo.Abp.ObjectExtending;
using Volo.Abp.PermissionManagement;
using Volo.Abp.SettingManagement;
using Unity.Notifications;
using Unity.Reporting;
using Unity.GrantManager.Permissions;
using Volo.Abp.Authorization.Permissions;

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

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        // Remove the retired grouping definitions from ABP's persisted permission tree.
        // The action permission identifiers and their assignments are retained.
        Configure<AbpPermissionOptions>(options =>
        {
            options.DeletedPermissions.Add(GrantApplicationPermissions.Assignments.Default);
            options.DeletedPermissions.Add(GrantApplicationPermissions.Reviews.Default);
            options.DeletedPermissions.Add(GrantApplicationPermissions.Approvals.Default);
        });
    }
}
