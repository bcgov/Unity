using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.GrantManager.Applications;

public class ApplicationTags : AuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid ApplicationId { get; set; }
    public virtual Application Application
    {
        set => _application = value;
        get => _application
               ?? throw new InvalidOperationException("Uninitialized property: " + nameof(Application));
    }
    private Application? _application;

    public string Text { get; set; } = string.Empty;
    public Guid? TenantId { get; set; }
}