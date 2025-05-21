using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Unity.GrantManager.Notifications.Settings;

public class DynamicUrl : AuditedAggregateRoot<Guid>
{
    public string KeyName { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

}
