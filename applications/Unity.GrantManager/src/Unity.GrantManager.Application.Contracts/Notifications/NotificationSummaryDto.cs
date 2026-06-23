using System;
using Unity.Notifications.Emails;
using Volo.Abp.Application.Dtos;

namespace Unity.GrantManager.Notifications;

public class NotificationSummaryDto : EntityDto<Guid>
{
    public Guid ApplicationId { get; set; }
    public string SubmissionReferenceNo { get; set; } = string.Empty;
    public string ApplicantName { get; set; } = string.Empty;
    public DateTime? SentDateTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
    public string ToAddress { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public RecipientType? Recipient { get; set; }
    public EmailType? EmailType { get; set; }
}
