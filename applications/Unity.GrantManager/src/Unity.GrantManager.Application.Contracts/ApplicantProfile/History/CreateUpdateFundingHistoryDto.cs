using System;

namespace Unity.GrantManager.ApplicantProfile;

public class CreateUpdateFundingHistoryDto
{
    public Guid? ApplicantId { get; set; }
    public string? GrantCategory { get; set; }
    public string? FundingYear { get; set; }
    public bool? RenewedFunding { get; set; }
    public decimal? ApprovedAmount { get; set; }
    public decimal? OneTimeConsideration { get; set; }    
    public decimal? ReconsiderationAmount { get; set; }
    public decimal? TotalGrantAmount { get; set; }
    public string? FundingNotes { get; set; }
}
