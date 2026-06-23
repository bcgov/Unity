using System;
using System.ComponentModel.DataAnnotations;

namespace Unity.GrantManager.Web.Views.Shared.Components.ApplicantPayments;

public class ApplicantPaymentsViewModel
{
    public Guid ApplicantId { get; set; }

    [Display(Name = "Total Approved Amount")]
    public decimal TotalApprovedAmount { get; set; }

    [Display(Name = "Total Paid Amount")]
    public decimal TotalPaidAmount { get; set; }

    [Display(Name = "Total Remaining Amount")]
    public decimal TotalRemainingAmount { get; set; }
}
