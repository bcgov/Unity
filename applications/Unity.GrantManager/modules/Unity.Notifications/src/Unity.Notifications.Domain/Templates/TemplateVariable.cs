using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.Notifications.Templates;

public class TemplateVariable : AuditedAggregateRoot<Guid>, IMultiTenant
{

    public Guid? TenantId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string MapTo { get; set; } = string.Empty;

}
