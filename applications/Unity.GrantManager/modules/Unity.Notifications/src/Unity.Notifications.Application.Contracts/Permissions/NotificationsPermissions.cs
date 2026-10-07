using Volo.Abp.Reflection;

namespace Unity.Notifications.Permissions;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Critical Code Smell", "S3218:Inner class members should not shadow outer class \"static\" or type members", Justification = "Constants File")]
public static class NotificationsPermissions
{
    public const string GroupName = "Notifications";
    public const string Settings = "SettingManagement.Notifications";

    public static class Email
    {
        public const string Default = "Notifications.Email";

        public static class Send
        {
            public const string Default = "Notifications.Email.Send";
            public const string SendBulk = "Notifications.Email.Send.Bulk";
            public const string Application = "Notifications.Email.Send.Application";
            public const string Applicant = "Notifications.Email.Send.Applicant";
        }

        public static class Create
        {
            public const string Default = "Notifications.Email.Create";
            public const string Application = "Notifications.Email.Create.Application";
            public const string Applicant = "Notifications.Email.Create.Applicant";
        }

        public static class Edit
        {
            public const string Default = "Notifications.Email.Edit";
            public const string Application = "Notifications.Email.Edit.Application";
            public const string Applicant = "Notifications.Email.Edit.Applicant";
        }

        public const string SendBulk = "Notifications.Email.SendBulk";
        public const string DeleteDraft = "Notifications.Email.DeleteDraft";
        public const string CancelScheduled = "Notifications.Email.CancelScheduled";
        public const string Schedule = "Notifications.Email.Schedule";
        public const string NotificationsTab = "Notifications.Form.Tab";
        public const string ScheduleCreate = "Notifications.Form.Email.Schedule.Create";
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
