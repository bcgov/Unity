using Unity.GrantManager.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace Unity.GrantManager.Permissions;

public class GrantManagerPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var grantManagerPermissionsGroup = context.AddGroup(GrantManagerPermissions.GroupName, L("Permission:GrantManagerManagement"));

        // Default grant manager user
        grantManagerPermissionsGroup.AddPermission(GrantManagerPermissions.Default, L("Permission:GrantManagerManagement.Default"));

        var organizationPermissions = grantManagerPermissionsGroup.AddPermission(GrantManagerPermissions.Organizations.Default, L("Permission:GrantApplicationManagement.Organizations.Default"));
        organizationPermissions.AddChild(GrantManagerPermissions.Organizations.ManageProfiles, L("Permission:GrantApplicationManagement.Organizations.ManageProfiles"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<GrantManagerResource>(name);
    }
}
