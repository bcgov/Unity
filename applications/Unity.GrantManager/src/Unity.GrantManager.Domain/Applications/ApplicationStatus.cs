using System;
using System.Collections.Generic;
using Unity.GrantManager.GrantApplications;
using Volo.Abp.Domain.Entities.Auditing;

namespace Unity.GrantManager.Applications;

public class ApplicationStatus : AuditedAggregateRoot<Guid>
{
    public string ExternalStatus { get; set; } = string.Empty;

    public string InternalStatus { get; set; } = string.Empty;

    public GrantApplicationState StatusCode { get; set; }

    // Navigation Property
    public virtual ICollection<Application>? Applications { get; set; }
}