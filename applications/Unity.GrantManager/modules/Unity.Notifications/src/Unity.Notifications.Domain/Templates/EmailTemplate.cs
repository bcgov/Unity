using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.Notifications.Templates;
public class EmailTemplate : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
  
    public Guid? TenantId { get; set; }
    public string Name { get; set; } = "";
    public string Description  { get; set; } = "";
    public string Subject  { get; set; } = "";

    public string BodyText { get; set; } = "";
    public string BodyHTML  { get; set; } = "";
    public string SendFrom   { get; set; } = "";

}
