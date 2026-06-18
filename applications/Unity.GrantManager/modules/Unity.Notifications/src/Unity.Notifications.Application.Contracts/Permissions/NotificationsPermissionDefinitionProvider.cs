using Unity.Notifications.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Features;
using Volo.Abp.Localization;
using Volo.Abp.SettingManagement;

namespace Unity.Notifications.Permissions;

public class NotificationsPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var notificationsPermissionsGroup = context.AddGroup(NotificationsPermissions.GroupName, L("Permission:Notifications"));

        var notificationsPermissions = notificationsPermissionsGroup.AddPermission(
                NotificationsPermissions.Email.Default, 
                L($"Permission:{NotificationsPermissions.Email.Default}"));
        
        notificationsPermissions.AddChild(
            NotificationsPermissions.Email.Send,
            L($"Permission:{NotificationsPermissions.Email.Send}"));

        var notificationListPermission = notificationsPermissionsGroup.AddPermission(
                NotificationsPermissions.NotificationList.Default,
                L($"Permission:{NotificationsPermissions.NotificationList.Default}"))
            .RequireFeatures("Unity.Notifications");

        notificationListPermission.AddChild(
            NotificationsPermissions.NotificationList.View,
            L($"Permission:{NotificationsPermissions.NotificationList.View}"));

        var settingManagement = context.GetGroup(SettingManagementPermissions.GroupName);
        settingManagement.AddPermission(NotificationsPermissions.Settings, L("Permission:NotificationsPermissions.Settings"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<NotificationsResource>(name);
    }
}
