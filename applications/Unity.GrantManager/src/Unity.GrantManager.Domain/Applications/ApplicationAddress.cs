using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.GrantManager.Applications;

public class ApplicationAddress : AuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid ApplicationId { get; set; }
    public string Street { get; set; } = String.Empty;
    public string? Unit { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public string? Postal { get; set; }
    public string AddressType { get; set; } = String.Empty;
    public Guid? TenantId { get; set; }
}
