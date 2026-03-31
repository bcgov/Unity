using Unity.Flex.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace Unity.Flex.Permissions;

public class FlexPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        context.AddGroup(FlexPermissions.GroupName, L("Permission:Flex"));

        var settingsMgmt = context.GetGroupOrNull("SettingManagement");
        if (settingsMgmt != null)
        {
            var configureWorksheet = settingsMgmt.AddPermission(
                FlexPermissions.Worksheets.Default,
                L("Permission:Flex.Worksheets")
            );
            configureWorksheet.AddChild(
                FlexPermissions.Worksheets.Delete,
                L("Permission:Flex.Worksheets.Delete")
            );
        }
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<FlexResource>(name);
    }
}
