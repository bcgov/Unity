using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Domain.Entities.Auditing;

namespace Unity.GrantManager.Applications;

public class FormSubmission : AuditedAggregateRoot<Guid>
{
    public Guid UserId { get; set; }
    public Guid ApplicationId { get; set; }

    public string ChefsSubmissionGuid { get; set; }
}
