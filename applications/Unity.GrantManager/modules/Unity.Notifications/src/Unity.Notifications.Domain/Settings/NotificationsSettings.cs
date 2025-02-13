namespace Unity.Notifications.Settings;

public static class NotificationsSettings
{
    public const string GroupName = "GrantManager.Notifications";

    public static class Mailing
    {
        public const string Default = "GrantManager.Notifications.Mailing";
        public const string DefaultFromAddress = "GrantManager.Notifications.Mailing.DefaultFromAddress";
        public const string EmailMaxRetryAttempts = "GrantManager.Notifications.Mailing.EmailMaxRetryAttempts";
    }
}
