using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Unity.GrantManager.Applicants;

public class ApplicantTenantMap : CreationAuditedAggregateRoot<Guid>
{
    public string OidcSubUsername { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; }
}
