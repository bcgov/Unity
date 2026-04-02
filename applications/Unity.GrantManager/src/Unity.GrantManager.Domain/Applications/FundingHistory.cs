using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.GrantManager.Applications;

public class FundingHistory : AuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? ApplicantId { get; set; }
    public string? GrantCategory { get; set; }
    public string? FundingYear { get; set; }
    public bool? RenewedFunding { get; set; }
    public decimal? ApprovedAmount { get; set; }
    public decimal? ReconsiderationAmount { get; set; }
    public decimal? OneTimeConsideration { get; set; }
    public decimal? TotalGrantAmount { get; set; }
    public string? FundingNotes { get; set; }
    public Guid? TenantId { get; set; }
}
