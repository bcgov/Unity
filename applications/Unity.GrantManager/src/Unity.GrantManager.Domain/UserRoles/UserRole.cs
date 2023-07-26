using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.GrantManager.GrantApplications;
using Volo.Abp.Domain.Entities.Auditing;

namespace Unity.GrantManager.ApplicationUserRoles;

public class UserRole : AuditedAggregateRoot<Guid>
{
    public Guid RoleId { get; set; }

    public Guid GrantProgramId { get; set; }
    
}
