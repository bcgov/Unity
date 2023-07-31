using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.GrantManager.GrantApplications;
using Volo.Abp.Domain.Entities.Auditing;

namespace Unity.GrantManager.ApplicationUserRoles;

public class UserTeam : AuditedAggregateRoot<Guid>
{
    public Guid TeamId { get; set; }

    public string OidcSub { get; set; }
    
}
