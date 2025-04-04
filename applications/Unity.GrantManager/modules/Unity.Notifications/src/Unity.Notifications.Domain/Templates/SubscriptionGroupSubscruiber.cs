using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;
using System.Collections.ObjectModel;
using Unity.Notifications.Templates;

namespace Unity.Notifications.Emails;
public class SubscriptionGroupSubscriber : AuditedAggregateRoot<Guid>, IMultiTenant
{
    
    public Guid? TenantId { get; set; }
    public Guid? GroupId  { get; set; }
    public Guid? SubscriberID  { get; set; }

    public virtual Collection<SubscriptionGroup>? SubscriptionGroups { get; private set; }

    public virtual Collection<Subscriber>? Subscribers  { get; private set; }


}
