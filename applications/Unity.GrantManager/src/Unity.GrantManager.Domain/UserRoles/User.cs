using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Domain.Entities.Auditing;

namespace Unity.GrantManager.ApplicationUserRoles;

public class User : AuditedAggregateRoot<Guid>
{
    public Guid UserRoleId { get; set; }
    public string Sub { get; set; }
    public string DisplayName { get; set; }
    public string Email { get; set; }
}
