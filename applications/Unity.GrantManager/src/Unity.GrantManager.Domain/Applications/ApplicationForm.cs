using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Domain.Entities.Auditing;

namespace Unity.GrantManager.Applications;

public class ApplicationForm : FullAuditedAggregateRoot<Guid>
{
    public Guid IntakeId { get; set; }
    public string ApplicationFormName { get; set; }

    public string? ApplicationFormDescription { get; set;}

    public string ChefsApplicationFormGuid { get; set; }

    public string ChefsCriteriaFormGuid { get; set; }
}
