using System;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Domain.Entities.Auditing;

namespace Unity.GrantManager.Applications;

public class ApplicationForm : FullAuditedAggregateRoot<Guid>
{
    public Guid IntakeId { get; set; }

    [Required]
    public string? ApplicationFormName { get; set; }

    public string? ApplicationFormDescription { get; set;}

    public string? ChefsApplicationFormGuid { get; set; }

    public string? ChefsCriteriaFormGuid { get; set; }

    public string? ApiKey { get; set; }
}
