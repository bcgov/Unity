using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Unity.Payments.Web.Pages.Payments
{
    public class PaymentsModel
    {
        [DisplayName("ApplicationPaymentRequest:Amount")]
        [Required]
        public decimal Amount { get; set; }
        [DisplayName("ApplicationPaymentRequest:Description")]
        [MaxLength(40)]
        public string? Description { get; set; }
        [Required(ErrorMessage = "This field is required.")]
        [DisplayName("ApplicationPaymentRequest:InvoiceNumber")]
        public string InvoiceNumber { get; set; } = string.Empty;
        public Guid CorrelationId { get; set; }
        [Required(ErrorMessage = "This field is required.")]
        [DisplayName("ApplicationPaymentRequest:SiteNumber")]
        public Guid SiteId { get; set; }
        public bool? Payable { get; set; }
        public bool RenderFormIoToHtml { get; set; } = false;
        public string? ApplicantName { get; set; }
        public string? SubmissionConfirmationCode { get; set; }
        public decimal PaymentThreshold { get; set; }

        [DisplayName("ApplicationPaymentRequest:SiteName")]
        public string SiteName { get; set; } = string.Empty;
        public List<string> ErrorList { get; set; } = new List<string> { };
        public bool DisableFields { get; set; } = false;
        public string? ContractNumber { get; set; }
        public string? SupplierNumber { get; set; }
        public string? SupplierName { get; set; }
        public decimal RemainingAmount { get; set; }
        public Guid? AccountCodingId { get; set; }
        public string? ParentReferenceNo { get; set; }
        public decimal? MaximumAllowedAmount { get; set; }
        public bool IsPartOfParentChildGroup { get; set; }
    }
}
