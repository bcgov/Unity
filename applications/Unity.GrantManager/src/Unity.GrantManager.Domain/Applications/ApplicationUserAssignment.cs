using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Domain.Entities.Auditing;

namespace Unity.GrantManager.Applications;

public class ApplicationUserAssignment : AuditedAggregateRoot<Guid>
{
    public Guid? TeamId { get; set; }

    public string OidcSub { get; set; } 

    public Guid? ApplicationFormId { get; set; }
    public Guid ApplicationId { get; set; }

    public string AssigneeDisplayName { get; set; }

    public DateTime AssignmentTime { get; set; }
       
}
