using Unity.Notifications.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;
using Volo.Abp.SettingManagement;

namespace Unity.Notifications.Permissions;

public class NotificationsPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var notificationsPermissionsGroup = context.AddGroup(NotificationsPermissions.GroupName, L("Permission:Notifications"));

        notificationsPermissionsGroup.AddNotifications_Email_Permissions();

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

[System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S125:Sections of code should not be commented out", Justification = "Configuration Code")]
public static class EmailPermissionDefinitionExtensions
{
    public static void AddNotifications_Email_Permissions(this PermissionGroupDefinition notificationsPermissionsGroup)
    {
        var email = notificationsPermissionsGroup.AddPermission(NotificationsPermissions.Email.Default, L($"Permission:{NotificationsPermissions.Email.Default}"));

        // Template attachments are not owner-scoped.
        email.AddUnityChild(NotificationsPermissions.Email.Send.Default);
        email.AddUnityChild(NotificationsPermissions.Email.SendBulk);

        AddOwnerPermissions(
            email,
            NotificationsPermissions.Email.Application.Default,
            NotificationsPermissions.Email.Application.Create,
            NotificationsPermissions.Email.Application.Edit,
            NotificationsPermissions.Email.Application.Send,
            NotificationsPermissions.Email.Application.Schedule,
            NotificationsPermissions.Email.Application.DeleteDraft,
            NotificationsPermissions.Email.Application.CancelScheduled);

        AddOwnerPermissions(
            email,
            NotificationsPermissions.Email.Applicant.Default,
            NotificationsPermissions.Email.Applicant.Create,
            NotificationsPermissions.Email.Applicant.Edit,
            NotificationsPermissions.Email.Applicant.Send,
            NotificationsPermissions.Email.Applicant.Schedule,
            NotificationsPermissions.Email.Applicant.DeleteDraft,
            NotificationsPermissions.Email.Applicant.CancelScheduled);
    }

    private static void AddOwnerPermissions(PermissionDefinition email, string view, params string[] operations)
    {
        var owner = email.AddUnityChild(view);
        foreach (var operation in operations)
        {
            owner.AddUnityChild(operation);
        }
    }
    public static PermissionDefinition AddUnityChild(this PermissionDefinition parent, string name)
    {
        return parent.AddChild(name, L($"Permission:{name}"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<NotificationsResource>(name);
    }
}
