using System;
using Volo.Abp.Application.Dtos;

namespace Unity.GrantManager.ApplicantProfile;

public class FundingHistoryDto : AuditedEntityDto<Guid>
{
    public Guid? ApplicantId { get; set; }
    public string? GrantCategory { get; set; }
    public int? FundingYear { get; set; }
    public bool? RenewedFunding { get; set; }
    public decimal? ApprovedAmount { get; set; }
    public decimal? ReconsiderationAmount { get; set; }
    public decimal? TotalGrantAmount { get; set; }
    public string? FundingNotes { get; set; }
}
