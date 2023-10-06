using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Unity.GrantManager.Applications;

public class AssessmentAttachment : AuditedAggregateRoot<Guid>
{
    public Guid AssessmentId { get; set; }
    public string S3ObjectKey { get; set; } = String.Empty;
    public Guid UserId { get; set; }
    public string FileName { get; set; } = String.Empty;
    public string? AttachedBy { get; set; }
    public DateTime Time { get; set; }
}

