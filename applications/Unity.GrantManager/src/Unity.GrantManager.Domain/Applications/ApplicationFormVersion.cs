using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.GrantManager.Applications;

public class ApplicationFormVersion : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid ApplicationFormId { get; set; }
    public string? ChefsApplicationFormGuid { get; set; }
    public string? ChefsFormVersionGuid { get; set; }
    public string? SubmissionHeaderMapping { get; set; }
    public string? AvailableChefsFields { get; set; }
    public int? Version { get; set; }
    public bool Published { get; set; }
    public Guid? TenantId { get; set; }
}
