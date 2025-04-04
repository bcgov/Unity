using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.Notifications.Templates;

public class Subscriber : AuditedAggregateRoot<Guid>, IMultiTenant
{
    protected Subscriber()
    {
        /* This constructor is for ORMs to be used while getting the entity from the database. */
    }

    public Guid? TenantId { get; set; }
    
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
