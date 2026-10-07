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
[System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S1481:Unused local variables should be removed", Justification = "Configuration Code")]
public static class EmailPermissionDefinitionExtensions
{
    public static void AddNotifications_Email_Permissions(this PermissionGroupDefinition notificationsPermissionsGroup)
    {
        var upx_Notifications_Email = notificationsPermissionsGroup.AddPermission(NotificationsPermissions.Email.Default, L($"Permission:{NotificationsPermissions.Email.Default}"));

        // EMAIL SEND
        var upx_Notifications_Email_Send                = upx_Notifications_Email.AddUnityChild(NotificationsPermissions.Email.Send.Default);
        var upx_Notifications_Email_Send_Application    = upx_Notifications_Email.AddUnityChild(NotificationsPermissions.Email.Send.Application);
        var upx_Notifications_Email_Send_Applicant      = upx_Notifications_Email.AddUnityChild(NotificationsPermissions.Email.Send.Applicant);
        
        // NOTE: Should be refactored under upx_Notifications_Email_Send in the future
        upx_Notifications_Email.AddUnityChild(NotificationsPermissions.Email.SendBulk);
        
        // EMAIL CREATE
        var upx_Notifications_Email_Create                = upx_Notifications_Email.AddUnityChild(NotificationsPermissions.Email.Create.Default);
        var upx_Notifications_Email_Create_Application    = upx_Notifications_Email.AddUnityChild(NotificationsPermissions.Email.Create.Application);
        var upx_Notifications_Email_Create_Applicant      = upx_Notifications_Email.AddUnityChild(NotificationsPermissions.Email.Create.Applicant);

        // EMAIL EDIT
        var upx_Notifications_Email_Edit                = upx_Notifications_Email.AddUnityChild(NotificationsPermissions.Email.Edit.Default);
        var upx_Notifications_Email_Edit_Application    = upx_Notifications_Email.AddUnityChild(NotificationsPermissions.Email.Edit.Application);
        var upx_Notifications_Email_Edit_Applicant      = upx_Notifications_Email.AddUnityChild(NotificationsPermissions.Email.Edit.Applicant);
    
        upx_Notifications_Email.AddUnityChild(
            NotificationsPermissions.Email.DeleteDraft);

        upx_Notifications_Email.AddUnityChild(
            NotificationsPermissions.Email.CancelScheduled);

        upx_Notifications_Email.AddUnityChild(
            NotificationsPermissions.Email.Schedule);
    }

    public static PermissionDefinition AddUnityChild(this PermissionDefinition parent, string name)
    {
        return parent.AddChild(name, LocalizableString.Create<NotificationsResource>(name));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<NotificationsResource>(name);
    }
}
