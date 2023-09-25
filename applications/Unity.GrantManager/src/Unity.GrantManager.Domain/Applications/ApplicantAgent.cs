using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Unity.GrantManager.Applications;

public class ApplicantAgent : AuditedAggregateRoot<Guid>
{
    public string OidcSubUser { get; set; } = string.Empty;
    public Guid ApplicantId { get; set; }
    public bool IsConfirmed { get; set; }

    public string RoleForApplicant { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

}
