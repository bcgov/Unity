using Microsoft.AspNetCore.Mvc;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Unity.GrantManager.Web.Pages.ApplicantHistory;

public class FundingHistoryModalViewModel
{
    [HiddenInput]
    public Guid ApplicantId { get; set; }

    [DisplayName("Grant Category")]
    public string? GrantCategory { get; set; }

    [DisplayName("Funding Year")]
    public int? FundingYear { get; set; }

    [DisplayName("Renewed Funding")]
    public bool? RenewedFunding { get; set; }

    [DisplayName("Approved Amount")]
    [DataType(DataType.Currency)]
    public decimal? ApprovedAmount { get; set; }

    [DisplayName("Reconsideration Amount")]
    [DataType(DataType.Currency)]
    public decimal? ReconsiderationAmount { get; set; }

    [DisplayName("Total Grant Amount")]
    [DataType(DataType.Currency)]
    public decimal? TotalGrantAmount { get; set; }

    [DisplayName("Funding Notes")]
    public string? FundingNotes { get; set; }
}
