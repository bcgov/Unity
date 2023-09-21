using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Unity.GrantManager.Applications;

public class Applicant : AuditedAggregateRoot<Guid>
{
    public string ApplicantName { get; set; } = string.Empty;
}
