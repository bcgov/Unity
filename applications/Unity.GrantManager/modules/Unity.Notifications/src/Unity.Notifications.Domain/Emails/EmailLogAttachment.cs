using System;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.Notifications.Emails;

public class EmailLogAttachment : AuditedAggregateRoot<Guid>, IMultiTenant
{
    // Foreign key to EmailLog
    public Guid EmailLogId { get; set; }

    // S3 storage properties
    public string S3ObjectKey { get; set; } = string.Empty;

    // File metadata
    public string? FileName { get; set; }

    [MaxLength(1024)]
    public string? DisplayName { get; set; }

    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }

    // Upload tracking
    public DateTime Time { get; set; }
    public Guid UserId { get; set; }

    // Multi-tenancy
    public Guid? TenantId { get; set; }
}
