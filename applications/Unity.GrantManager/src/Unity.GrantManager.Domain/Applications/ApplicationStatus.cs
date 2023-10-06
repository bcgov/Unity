using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Unity.GrantManager.Applications;

public class ApplicationStatus : AuditedAggregateRoot<Guid>
{
    public string ExternalStatus { get; set; } = string.Empty;

    public string InternalStatus { get; set; } = string.Empty;

    public string StatusCode { get; set; } = string.Empty;
}