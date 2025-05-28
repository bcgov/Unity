using System;
using System.Text.Json.Serialization;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.Notifications.Templates;

public class SubscriptionGroupSubscription : AuditedAggregateRoot<Guid>, IMultiTenant
{
    protected SubscriptionGroupSubscription()
    {
        /* This constructor is for ORMs to be used while getting the entity from the database. */
    }

    public Guid? TenantId { get; set; }

    // Navigation
    public Guid? GroupId { get; set; }        
    [JsonIgnore]
    public virtual SubscriptionGroup SubscriptionGroup
    {
        set => _subscriptionGroup = value;
        get => _subscriptionGroup
               ?? throw new InvalidOperationException("Uninitialized property: " + nameof(SubscriptionGroup));
    }
    private SubscriptionGroup? _subscriptionGroup;

    // Navigation
    public Guid? SubscriberId { get; set; }
    [JsonIgnore]
    public virtual Subscriber Subscriber
    {
        set => _subscriber = value;
        get => _subscriber
               ?? throw new InvalidOperationException("Uninitialized property: " + nameof(Subscriber));
    }
    private Subscriber? _subscriber;
}
