using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.GrantManager.Contacts;

public class ContactLink : AuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid ContactId { get; set; }
    public string RelatedEntityType { get; set; } = string.Empty;
    public Guid RelatedEntityId { get; set; }    
    public string? Role { get; set; }
    public bool IsPrimary { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public Guid? TenantId { get; set; }
}
