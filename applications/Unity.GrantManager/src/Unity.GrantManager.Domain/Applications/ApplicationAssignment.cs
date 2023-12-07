using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Unity.GrantManager.Applications;

public class ApplicationAssignment : AuditedAggregateRoot<Guid>
{        
    public Guid ApplicationId { get; set; }
    public Guid AssigneeId { get; set; }
}
