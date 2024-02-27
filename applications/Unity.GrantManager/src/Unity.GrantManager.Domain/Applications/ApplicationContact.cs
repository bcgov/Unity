using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.GrantManager.Applications;

public class ApplicationContact : AuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid ApplicationId { get; set; }
    public string ContactType { get; set; } = string.Empty;
    public string ContactFullName { get; set; } = string.Empty;
    public string? ContactTitle { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactMobilePhone { get; set; }
    public string? ContactWorkPhone { get; set; }
    public Guid? TenantId { get; set; }
}
