using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Domain.Entities.Auditing;

namespace Unity.GrantManager.ApplicationUserRoles;

public class User : AuditedAggregateRoot<Guid>
{
    public string OidcSub { get; set; }
    public string OidcDisplayName { get; set; }
    public string OidcEmail { get; set; }
    public string LegalName { get; set; }
    public string PreferredFirstName { get; set; }
    public string PreferredLastName { get; set; }
    public string Phone {get; set; }
}
