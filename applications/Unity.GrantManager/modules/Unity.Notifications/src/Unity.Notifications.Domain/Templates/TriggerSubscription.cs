using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;
using System.Collections.ObjectModel;

namespace Unity.Notifications.Templates; 
public class TriggerSubscription : AuditedAggregateRoot<Guid>, IMultiTenant
{

    public Guid? TenantId { get; set; }
    public Guid? TriggerId { get; set; } 
    public Guid? TemplateId { get; set; } 
    public Guid? SubscriptionGroupId { get; set; }

    public virtual Collection<Trigger>? Trigger  { get; private set; }
    public virtual Collection<EmailTemplate>? EmailTemplate { get; private set; }
    public virtual Collection<SubscriptionGroup>? SubscriptionGroup { get; private set; }


}
