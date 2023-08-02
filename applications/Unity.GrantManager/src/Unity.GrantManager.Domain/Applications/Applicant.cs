using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Domain.Entities.Auditing;

namespace Unity.GrantManager.Applications;

public class Applicant : AuditedAggregateRoot<Guid>
{
    public string ApplicantName { get; set; }
}
