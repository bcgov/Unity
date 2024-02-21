using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Unity.GrantManager.Intakes;

public class ChefsMissedSubmission : AuditedAggregateRoot<Guid>
{
    public string? ChefsSubmissionGuids { get; set; }

    public string? ChefsApplicationFormGuid { get; set; }

    public Guid? TenantId { get; set; }
}
