using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Unity.GrantManager.Assessments;

public class AssessmentComment : AuditedAggregateRoot<Guid>
{
    public Guid AssessmentId { get; set; }

    public string Comment { get; set; } = string.Empty;
}
