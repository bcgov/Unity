using Unity.Notifications.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;
using Volo.Abp.SettingManagement;

namespace Unity.Notifications.Permissions;

public class NotificationsPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        context.AddGroup(NotificationsPermissions.GroupName, L("Permission:Notifications"));

        var settingManagement = context.GetGroup(SettingManagementPermissions.GroupName);
        settingManagement.AddPermission(NotificationsPermissions.Settings, L("Permission:NotificationsPermissions.Settings"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<NotificationsResource>(name);
    }
}
