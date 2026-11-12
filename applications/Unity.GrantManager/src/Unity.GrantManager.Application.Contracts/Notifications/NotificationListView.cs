namespace Unity.GrantManager.Notifications;

// The "Type" selector on the Notification List page.
public enum NotificationListView
{
    // Application-level and applicant-level notifications.
    Both = 0,

    // Notifications tied to an application (EmailLog.ApplicationId is set).
    Application = 1,

    // Applicant-level notifications that have no application (EmailLog.ApplicationId is empty).
    Applicant = 2
}
