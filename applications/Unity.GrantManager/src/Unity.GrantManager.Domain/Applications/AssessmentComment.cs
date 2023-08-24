using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Unity.GrantManager.Applications;

public class AssessmentComment : AuditedAggregateRoot<Guid>
{
    public Guid ApplicationFormSubmissionId { get; set; }

    public string Comment { get; set; } = string.Empty;
}
