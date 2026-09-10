using System;

namespace Unity.Notifications.Logs;

public class NotificationLogCreatedEto
{
    public Guid Id { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? UserId { get; set; }
    public NotificationLogType NotificationType { get; set; }
    public NotificationLogChannel Channel { get; set; }
    public NotificationLogSeverity Severity { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }
    public DateTime CreationTime { get; set; }
}
