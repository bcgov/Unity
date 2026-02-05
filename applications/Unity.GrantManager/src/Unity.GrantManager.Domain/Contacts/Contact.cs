using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.GrantManager.Contacts;

public class Contact : AuditedAggregateRoot<Guid>, IMultiTenant
{
    public string Name { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Email { get; set; }
    public string? HomePhoneNumber { get; set; }
    public string? MobilePhoneNumber { get; set; }
    public string? WorkPhoneNumber { get; set; } 
    public string? WorkPhoneExtension { get; set; }
    public Guid? TenantId { get; set; }
}
	
