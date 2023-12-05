using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Unity.GrantManager.Applications;

public class ApplicationUserAssignment : AuditedAggregateRoot<Guid>
{        
    public Guid ApplicationId { get; set; }
    public Guid AssigneeId { get; set; }
}
