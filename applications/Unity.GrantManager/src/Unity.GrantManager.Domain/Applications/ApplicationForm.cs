using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Unity.GrantManager.Applications;

public class ApplicationForm : FullAuditedAggregateRoot<Guid>
{
    public Guid IntakeId { get; set; }
    public string ApplicationFormName { get; set; } = string.Empty;

    public string? ApplicationFormDescription { get; set;}

    public string ChefsApplicationFormGuid { get; set; } = string.Empty;

    public string ChefsCriteriaFormGuid { get; set; } = string.Empty;
}
