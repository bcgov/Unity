using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Domain.Entities.Auditing;

namespace Unity.GrantManager.Applications;

public class ApplicantAgent : AuditedAggregateRoot<Guid>
{
    public string OidcSubUser { get; set; }
    public Guid ApplicantId { get; set; }
    public Boolean IsConfirmed { get; set; }

    public string RoleForApplicant { get; set; }

    public string Phone { get; set; }

}
