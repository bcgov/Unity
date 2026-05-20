using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Unity.Payments.Web.Pages.Payments
{
    public class HistoricalPaymentsModel : IPaymentFormItem
    {
        // IPaymentFormItem
        public Guid CorrelationId { get; set; }
        [Required(ErrorMessage = "This field is required.")]
        [DisplayName("ApplicationPaymentRequest:InvoiceNumber")]
        public string InvoiceNumber { get; set; } = string.Empty;
        [Required]
        [DisplayName("ApplicationPaymentRequest:Amount")]
        public decimal Amount { get; set; }
        public string? ParentReferenceNo { get; set; }
        public string? SubmissionConfirmationCode { get; set; }
        public decimal? MaximumAllowedAmount { get; set; }
        public bool IsPartOfParentChildGroup { get; set; }
        public decimal? ParentApprovedAmount { get; set; }

        [Required(ErrorMessage = "Paid Date is required.")]
        [DisplayName("ApplicationHistoricalPaymentRequest:PaidDate")]
        public string PaidDate { get; set; } = string.Empty;

        [DisplayName("ApplicationPaymentRequest:Description")]
        [MaxLength(40)]
        public string? Description { get; set; }

        public string? ApplicantName { get; set; }
        public string? ContractNumber { get; set; }
        public decimal RemainingAmount { get; set; }
        public List<string> ErrorList { get; set; } = [];
        public bool DisableFields { get; set; } = false;
        public string? SupplierName { get; set; }
        public string? SupplierNumber { get; set; }
        public Guid? SiteId { get; set; }
        public string? SiteName { get; set; }
        public Guid? AccountCodingId { get; set; }
    }
}
