using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Unity.GrantManager.Applications;

public class ApplicantAgent : AuditedAggregateRoot<Guid>
{
    public string? OidcSubUser { get; set; }
    public Guid ApplicantId { get; set; }
    public Guid ApplicationId { get; set; }
    public bool IsConfirmed { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public string RoleForApplicant { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int ContactOrder { get; set; } = 0;
    public string Phone { get; set; } = string.Empty;
    public string Phone2 { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
}
