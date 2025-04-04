using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.Notifications.Templates;
public class SubscriptionGroup : AuditedAggregateRoot<Guid>, IMultiTenant
{

    public Guid? TenantId { get; set; }
    public string FirstName  { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";

}
