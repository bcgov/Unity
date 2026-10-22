using System;
using Unity.Notifications.Permissions;

namespace Unity.Notifications.Emails;

// Values match TemplateTypes so a template type and an owner type are interchangeable.
public static class EmailOwnerTypes
{
    public const string Application = "Application";
    public const string Applicant = "Applicant";

    public static string FromIds(Guid applicantId) => applicantId == Guid.Empty ? Application : Applicant;
}

public enum EmailOperation
{
    View,
    Create,
    Edit,
    Send,
    Schedule,
    DeleteDraft,
    CancelScheduled
}

public static class EmailPermissionMap
{
    public static string Get(string ownerType, EmailOperation operation)
    {
        return ownerType switch
        {
            EmailOwnerTypes.Application => operation switch
            {
                EmailOperation.View => NotificationsPermissions.Email.Application.Default,
                EmailOperation.Create => NotificationsPermissions.Email.Application.Create,
                EmailOperation.Edit => NotificationsPermissions.Email.Application.Edit,
                EmailOperation.Send => NotificationsPermissions.Email.Application.Send,
                EmailOperation.Schedule => NotificationsPermissions.Email.Application.Schedule,
                EmailOperation.DeleteDraft => NotificationsPermissions.Email.Application.DeleteDraft,
                EmailOperation.CancelScheduled => NotificationsPermissions.Email.Application.CancelScheduled,
                _ => throw new ArgumentOutOfRangeException(nameof(operation))
            },
            EmailOwnerTypes.Applicant => operation switch
            {
                EmailOperation.View => NotificationsPermissions.Email.Applicant.Default,
                EmailOperation.Create => NotificationsPermissions.Email.Applicant.Create,
                EmailOperation.Edit => NotificationsPermissions.Email.Applicant.Edit,
                EmailOperation.Send => NotificationsPermissions.Email.Applicant.Send,
                EmailOperation.Schedule => NotificationsPermissions.Email.Applicant.Schedule,
                EmailOperation.DeleteDraft => NotificationsPermissions.Email.Applicant.DeleteDraft,
                EmailOperation.CancelScheduled => NotificationsPermissions.Email.Applicant.CancelScheduled,
                _ => throw new ArgumentOutOfRangeException(nameof(operation))
            },
            _ => throw new ArgumentOutOfRangeException(nameof(ownerType))
        };
    }
}
