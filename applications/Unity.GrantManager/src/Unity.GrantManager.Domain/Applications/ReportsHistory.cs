using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.GrantManager.Applications;

public class ReportsHistory : AuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? ApplicantId { get; set; }
    public string? FiscalYear { get; set; }
    public DateTime? ReportDate { get; set; }
    public bool? Outstanding { get; set; }
    public bool? IncompleteReport { get; set; }
    public string? Note { get; set; }
    public Guid? TenantId { get; set; }
}
