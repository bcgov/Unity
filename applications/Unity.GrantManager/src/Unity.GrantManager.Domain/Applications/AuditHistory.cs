using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.GrantManager.Applications;

public class AuditHistory : AuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? ApplicantId { get; set; }
    public string? AuditTrackingNumber { get; set; }
    public DateTime? AuditDate { get; set; }
    public string? AuditNote { get; set; }
    public Guid? TenantId { get; set; }
}
