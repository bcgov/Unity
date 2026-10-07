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

        // Template attachments are not owner-scoped.
        public static class Send
        {
            public const string Default = "Notifications.Email.Send";
        }

        public static class Application
        {
            public const string Default = "Notifications.Email.Application";
            public const string Create = "Notifications.Email.Application.Create";
            public const string Edit = "Notifications.Email.Application.Edit";
            public const string Send = "Notifications.Email.Application.Send";
            public const string Schedule = "Notifications.Email.Application.Schedule";
            public const string DeleteDraft = "Notifications.Email.Application.DeleteDraft";
            public const string CancelScheduled = "Notifications.Email.Application.CancelScheduled";
        }

        public static class Applicant
        {
            public const string Default = "Notifications.Email.Applicant";
            public const string Create = "Notifications.Email.Applicant.Create";
            public const string Edit = "Notifications.Email.Applicant.Edit";
            public const string Send = "Notifications.Email.Applicant.Send";
            public const string Schedule = "Notifications.Email.Applicant.Schedule";
            public const string DeleteDraft = "Notifications.Email.Applicant.DeleteDraft";
            public const string CancelScheduled = "Notifications.Email.Applicant.CancelScheduled";
        }

        public const string SendBulk = "Notifications.Email.SendBulk";
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