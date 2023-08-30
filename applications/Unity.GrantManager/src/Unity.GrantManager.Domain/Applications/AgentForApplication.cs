using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Unity.GrantManager.Applications;

public class AgentForApplication : AuditedAggregateRoot<Guid>
{
    public Guid ApplicantAgentId { get; set; }
    public Guid ChefsFormSubmissionId { get; set; }
}
