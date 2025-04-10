using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.Notifications.Templates;

public class TemplateVariable : AuditedAggregateRoot<Guid>, IMultiTenant
{
    protected TemplateVariable()
    {
        /* This constructor is for ORMs to be used while getting the entity from the database. */
    }

    public Guid? TenantId { get; set; }

    public string Name { get; set; } = string.Empty;
    public bool Active { get; set; } = true;
    public string InternalName { get; set; } = string.Empty;
}
