using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;
using Unity.GrantManager.GlobalTag;

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
    public Guid TagId { get; set; }

    public virtual Tag Tag
    {
        set => _tag = value;
        get => _tag
               ?? throw new InvalidOperationException("Uninitialized property: " + nameof(Tag ));
    }
    private Tag? _tag;
    public Guid? TenantId { get; set; }
}