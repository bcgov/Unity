using Volo.Abp.Reflection;

namespace Unity.Notifications.Permissions;

public static class NotificationsPermissions
{
    public const string GroupName = "Notifications";
    public const string Settings = "SettingManagement.Notifications";

    public static class Email
    {
        public const string Default = "Notifications.Email";
        public const string Send = "Notifications.Email.Send";
        public const string Delete = "Notifications.Email.Delete";
        public const string Schedule = "Notifications.Email.Schedule";
        public const string NotificationsTab = "Notifications.Form.Tab";
        public const string ScheduleCreate = "Notifications.Form.Email.Schedule.Create";
        public const string ScheduleDelete = "Notifications.Form.Email.Schedule.Delete";
        public const string ScheduleCancel = "Notifications.Form.Email.Schedule.Cancel";
    }

    public static class NotificationList
    {
        public const string Default = "Notifications.NotificationList";
        public const string View = "Notifications.NotificationList.View";
    }

    public static string[] GetAll()
    {
        return ReflectionHelper.GetPublicConstantsRecursively(typeof(NotificationsPermissions));
    }
}
