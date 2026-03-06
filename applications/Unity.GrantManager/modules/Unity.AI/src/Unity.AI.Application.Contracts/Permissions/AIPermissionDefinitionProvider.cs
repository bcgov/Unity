using Unity.AI.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace Unity.AI.Permissions;

public class AIPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var aiPermissionsGroup = context.AddGroup(AIPermissions.GroupName, L("Permission:AI"));

        aiPermissionsGroup.AddPermission(AIPermissions.Default.Management, L("Permission:AI.Default"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<AIResource>(name);
    }
}
