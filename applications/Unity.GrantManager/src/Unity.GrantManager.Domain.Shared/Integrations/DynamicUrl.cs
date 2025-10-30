using System;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.GrantManager.Integrations;

public class DynamicUrl : FullAuditedEntity<Guid>, IMultiTenant, IHasConcurrencyStamp
{
    public string ConcurrencyStamp { get; set; } = string.Empty;
    public string KeyName { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid? TenantId { get; set; }
}
