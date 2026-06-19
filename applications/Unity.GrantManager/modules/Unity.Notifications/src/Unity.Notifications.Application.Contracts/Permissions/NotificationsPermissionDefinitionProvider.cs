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
            NotificationsPermissions.Email.Delete,
            L($"Permission:{NotificationsPermissions.Email.Delete}"));

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
            NotificationsPermissions.Email.ScheduleDelete,
            L($"Permission:{NotificationsPermissions.Email.ScheduleDelete}"));

        scheduleNotificationsPermissions.AddChild(
            NotificationsPermissions.Email.ScheduleCancel,
            L($"Permission:{NotificationsPermissions.Email.ScheduleCancel}"));

        var settingManagement = context.GetGroup(SettingManagementPermissions.GroupName);
        settingManagement.AddPermission(NotificationsPermissions.Settings, L("Permission:NotificationsPermissions.Settings"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<NotificationsResource>(name);
    }
}
