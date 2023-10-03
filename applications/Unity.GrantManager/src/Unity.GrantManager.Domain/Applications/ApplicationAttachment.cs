using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Unity.GrantManager.Applications;

public class ApplicationAttachment : AuditedAggregateRoot<Guid>
{
    public Guid ApplicationId { get; set; }
    public Guid S3Guid { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public DateTime Time { get; set; }
}
