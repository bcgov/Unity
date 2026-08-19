using System;
using Volo.Abp.Application.Dtos;

namespace Unity.Notifications.Logs;

public class GetNotificationLogsInput : PagedAndSortedResultRequestDto
{
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public NotificationLogType? NotificationType { get; set; }
    public NotificationLogSeverity? Severity { get; set; }
    public NotificationLogChannel? Channel { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? UserId { get; set; }
    public string? SearchText { get; set; }
}
