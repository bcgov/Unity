using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.GrantManager.Contacts;

public class Contact : AuditedAggregateRoot<Guid>, IMultiTenant
{
    public bool Primary { get; set; } = false;
    public string CorrelationType { get; set; } = string.Empty;
    public Guid CorrelationId { get; set; }
    public ContactType Type { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Email { get; set; }
    public string? MobilePhoneNumber { get; set; }
    public string? WorkPhoneNumber { get; set; }
    public string TypeCorrelation { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Display { get; set; } = string.Empty;
    public Guid? TenantId { get; set; }
}
