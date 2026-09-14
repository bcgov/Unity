using System;
using Unity.Notifications.Emails;

namespace Unity.GrantManager.Web.Controllers;

public class NotificationEmailModalViewModel
{
    public Guid ApplicationId { get; set; }
    public Guid ApplicantId { get; set; }
    public Guid? TenantId { get; set; }
    public string BodyType { get; set; } = string.Empty;
    public Guid CurrentUserId { get; set; }
    public EmailHistoryDto? SelectedEmail { get; set; }
}
