using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Unity.GrantManager.Applications;

public class ApplicationFormSubmission : AuditedAggregateRoot<Guid>
{
    public string OidcSub { get; set; } = string.Empty;
    public Guid ApplicantId { get; set; }

    public Guid ApplicationFormId { get; set; }

    public string ChefsSubmissionGuid { get; set; } = string.Empty;
}
