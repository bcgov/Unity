using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.ComponentModel.DataAnnotations;

namespace Unity.Payments.Web.Views.Shared.Components.PaymentInfo
{
    public class PaymentInfoViewModel : PageModel
    {
        [Display(Name = "PaymentInfoView:PaymentInfo.RequestedAmount")]
        public decimal? RequestedAmount { get; set; }

        [Display(Name = "PaymentInfoView:PaymentInfo.RecommendedAmount")]
        public decimal? RecommendedAmount { get; set; }

        [Display(Name = "PaymentInfoView:PaymentInfo.ApprovedAmount")]
        public decimal? ApprovedAmount { get; set; }

        [Display(Name = "PaymentInfoView:PaymentInfo.TotalPendingAmounts")]
        public decimal? TotalPendingAmounts { get; set; }

        [Display(Name = "PaymentInfoView:PaymentInfo.TotalPaid")]
        public decimal? TotalPaid { get; set; }

        [Display(Name = "PaymentInfoView:PaymentInfo.RemainingAmount")]
        public decimal? RemainingAmount { get; set; }

        public Guid ApplicationId { get; set; }
        public Guid ApplicationFormVersionId { get; set; } 
    }
}
