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

        notificationsPermissions.AddChild(
            NotificationsPermissions.Email.DeleteDraft,
            L($"Permission:{NotificationsPermissions.Email.DeleteDraft}"));

        notificationsPermissions.AddChild(
            NotificationsPermissions.Email.CancelScheduled,
            L($"Permission:{NotificationsPermissions.Email.CancelScheduled}"));

        notificationsPermissions.AddChild(
            NotificationsPermissions.Email.Schedule,
            L($"Permission:{NotificationsPermissions.Email.Schedule}"));


        var scheduleNotificationsPermissions = notificationsPermissionsGroup.AddPermission(
                NotificationsPermissions.Email.NotificationsTab,
                L($"Permission:{NotificationsPermissions.Email.NotificationsTab}"));

        scheduleNotificationsPermissions.AddChild(
            NotificationsPermissions.Email.ScheduleCreate,
            L($"Permission:{NotificationsPermissions.Email.ScheduleCreate}"));

        scheduleNotificationsPermissions.AddChild(
            NotificationsPermissions.Email.ScheduleCancel,
            L($"Permission:{NotificationsPermissions.Email.ScheduleCancel}"));

        var settingManagement = context.GetGroup(SettingManagementPermissions.GroupName);
        settingManagement.AddPermission(NotificationsPermissions.Settings, L("Permission:NotificationsPermissions.Settings"));

        var notificationListPermissions = notificationsPermissionsGroup.AddPermission(
                NotificationsPermissions.NotificationList.Default,
                L($"Permission:{NotificationsPermissions.NotificationList.Default}"));

        notificationListPermissions.AddChild(
            NotificationsPermissions.NotificationList.View,
            L($"Permission:{NotificationsPermissions.NotificationList.View}"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<NotificationsResource>(name);
    }
}
