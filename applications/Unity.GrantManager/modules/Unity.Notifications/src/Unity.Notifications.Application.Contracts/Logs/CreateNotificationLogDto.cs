using System;

namespace Unity.Notifications.Logs;

public class CreateNotificationLogDto
{
    public Guid? TenantId { get; set; }
    public Guid? UserId { get; set; }
    public Guid? SenderUserId { get; set; }
    public string? SenderDisplayName { get; set; }
    public NotificationLogType NotificationType { get; set; }
    public NotificationLogChannel Channel { get; set; }
    public NotificationLogSeverity Severity { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string? SourceReference { get; set; }
    public string? PayloadJson { get; set; }
    public string? CorrelationId { get; set; }
    public bool IsDeliveredRealtime { get; set; }
    public string? DeliveryTarget { get; set; }
    public string? ExceptionType { get; set; }
    public string? ExceptionMessage { get; set; }
    public string? StackExcerpt { get; set; }
    public string? CommitSha { get; set; }
    public string? Environment { get; set; }
}