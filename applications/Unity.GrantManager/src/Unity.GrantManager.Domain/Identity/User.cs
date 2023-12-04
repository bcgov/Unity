using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Unity.GrantManager.Identity;

public class User : AuditedAggregateRoot<Guid>
{
    public string OidcSub { get; set; } = string.Empty;
    public string OidcDisplayName { get; set; } = string.Empty;
    public Guid CorrelationId { get; set; }
}
