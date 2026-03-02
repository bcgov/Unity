using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;
using Volo.Abp.TenantManagement.Localization;

namespace Unity.TenantManagement;

public class UnityTenantManagementPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        // Don't redefine permissions that already exist in the base ABP TenantManagement module
        // Only add your custom permissions here
        var tenantManagementGroup = context.GetGroupOrNull(TenantManagementPermissions.GroupName);
        
        if (tenantManagementGroup == null)
        {
            context.AddGroup(TenantManagementPermissions.GroupName, L("Permission:TenantManagement"));
        }
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<AbpTenantManagementResource>(name);
    }
}
