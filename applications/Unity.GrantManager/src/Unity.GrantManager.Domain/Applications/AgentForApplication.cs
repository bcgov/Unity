using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Domain.Entities.Auditing;

namespace Unity.GrantManager.Applications;

public class AgentForApplication : AuditedAggregateRoot<Guid>
{
    public Guid ApplicantAgentId { get; set; }
    public Guid ChefsFormSubmissionId { get; set; }
}
