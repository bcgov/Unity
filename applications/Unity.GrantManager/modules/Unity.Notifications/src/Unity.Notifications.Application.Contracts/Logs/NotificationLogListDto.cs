using System;

namespace Unity.Notifications.Logs;

public class NotificationLogListDto
{
    public Guid Id { get; set; }
    public DateTime CreationTime { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? UserId { get; set; }
    public string UserDisplayName { get; set; } = string.Empty;
    public NotificationLogType NotificationType { get; set; }
    public NotificationLogSeverity Severity { get; set; }
    public NotificationLogChannel Channel { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }
    public bool IsDeliveredRealtime { get; set; }
}
