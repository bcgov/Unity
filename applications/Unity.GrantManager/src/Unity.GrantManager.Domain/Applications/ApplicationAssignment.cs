using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.GrantManager.Applications;

public class ApplicationAssignment : AuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid ApplicationId { get; set; }
    public Guid AssigneeId { get; set; }
    public Guid? TenantId { get; set; }
    public string? Role { get; set; } = string.Empty;
}
