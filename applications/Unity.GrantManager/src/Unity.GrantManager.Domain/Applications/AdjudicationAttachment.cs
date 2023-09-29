using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Domain.Entities.Auditing;

namespace Unity.GrantManager.Applications;

public class AdjudicationAttachment : AuditedAggregateRoot<Guid>
{
    public Guid AdjudicationId { get; set; }
    public string S3ObjectKey { get; set; } = String.Empty;
    public Guid UserId { get; set; }
    public string FileName { get; set; } = String.Empty;
    public string? AttachedBy { get; set; }
    public DateTime Time { get; set; }
}

