using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.GrantManager.Applications;

public class IssueTracking : AuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? ApplicantId { get; set; }
    public string? Year { get; set; }
    public string? IssueHeading { get; set; }
    public string? IssueDescription { get; set; }
    public bool? Resolved { get; set; }
    public string? ResolutionNote { get; set; }
    public Guid? TenantId { get; set; }
}
