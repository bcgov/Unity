using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.Notifications.Templates;
public class TemplateVariable  : AuditedAggregateRoot<Guid>, IMultiTenant
{
 
    public Guid? TenantId { get; set; }
    public string Name { get; set; } = "";
    public bool Active  { get; set; } = true;
    public string InternalName { get; set; } = "";

}
