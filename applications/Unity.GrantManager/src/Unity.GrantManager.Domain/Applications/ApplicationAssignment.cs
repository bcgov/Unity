using System;
using Unity.GrantManager.Identity;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.GrantManager.Applications;

public class ApplicationAssignment : AuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid ApplicationId { get; set; }
    public virtual Application Application
    {
        set => _application = value;
        get => _application
               ?? throw new InvalidOperationException("Uninitialized property: " + nameof(Application));
    }
    private Application? _application;

    public Guid AssigneeId { get; set; }
    public virtual Person Assignee
    {
        set => _assignee = value;
        get => _assignee
               ?? throw new InvalidOperationException("Uninitialized property: " + nameof(Assignee));
    }
    private Person? _assignee;

    public Guid? TenantId { get; set; }
    public string? Duty  { get; set; } = string.Empty;
}
