using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.GrantManager.Identity;

public class Person : AuditedAggregateRoot<Guid>, IMultiTenant
{
    public new Guid Id { get => base.Id; set => base.Id = value; }
    public string OidcSub { get; set; } = string.Empty;
    public string OidcDisplayName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Badge { get; set; } = string.Empty;
    public Guid? TenantId { get; set; }
}
