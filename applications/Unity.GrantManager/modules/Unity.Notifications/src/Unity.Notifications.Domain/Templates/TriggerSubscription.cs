using System;
using System.Text.Json.Serialization;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.Notifications.Templates;

public class TriggerSubscription : AuditedAggregateRoot<Guid>, IMultiTenant
{
    protected TriggerSubscription()
    {
        /* This constructor is for ORMs to be used while getting the entity from the database. */
    }

    public Guid? TenantId { get; set; }

    // Navigation
    public Guid TriggerId { get; set; }
    [JsonIgnore]
    public virtual Trigger Trigger
    {
        set => _trigger = value;
        get => _trigger
               ?? throw new InvalidOperationException("Uninitialized property: " + nameof(Trigger));
    }
    private Trigger? _trigger;

    // Navigation
    public Guid TemplateId { get; set; }
    [JsonIgnore]
    public virtual EmailTemplate EmailTemplate
    {
        set => _emailTemplate = value;
        get => _emailTemplate
               ?? throw new InvalidOperationException("Uninitialized property: " + nameof(EmailTemplate));
    }
    private EmailTemplate? _emailTemplate;

    // Navigation
    public Guid SubscriptionGroupId { get; set; }
    [JsonIgnore]
    public virtual SubscriptionGroup SubscriptionGroup
    {
        set => _subscriptionGroup = value;
        get => _subscriptionGroup
               ?? throw new InvalidOperationException("Uninitialized property: " + nameof(SubscriptionGroup));
    }
    private SubscriptionGroup? _subscriptionGroup;
}
