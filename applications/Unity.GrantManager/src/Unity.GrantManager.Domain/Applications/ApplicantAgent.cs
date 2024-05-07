using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.GrantManager.Applications;

public class ApplicantAgent : AuditedAggregateRoot<Guid>, IMultiTenant
{
    public string? OidcSubUser { get; set; }
    public Guid ApplicantId { get; set; }
    public Guid ApplicationId { get; set; }
    public virtual Application Application
    {
        set => _application = value;
        get => _application
               ?? throw new InvalidOperationException("Uninitialized property: " + nameof(Application));
    }
    private Application? _application;

    public bool IsConfirmed { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public string RoleForApplicant { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int ContactOrder { get; set; } = 0;
    public string Phone { get; set; } = string.Empty;
    public string Phone2 { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public Guid? TenantId { get; set; }
}
