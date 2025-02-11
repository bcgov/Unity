using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System;
using System.Collections.Generic;
using Unity.Payments.Enums;

namespace Unity.Payments.Web.Pages.PaymentApprovals
{
    public class PaymentsApprovalModel
    {
        [Required]
        public Guid Id { get; set; }

        [DisplayName("ApplicationPaymentStatusRequest:Id")]
        [Required]
        public string ReferenceNumber { get; set; } = string.Empty;

        [DisplayName("ApplicationPaymentStatusRequest:Amount")]
        [Required]
        public decimal Amount { get; set; }
        [DisplayName("ApplicationPaymentStatusRequest:Description")]
        public string? Description { get; set; }
        [Required(ErrorMessage = "This field is required.")]
        [DisplayName("ApplicationPaymentStatusRequest:InvoiceNumber")]
        public string InvoiceNumber { get; set; } = string.Empty;
        public Guid CorrelationId { get; set; }
        [Required(ErrorMessage = "This field is required.")]
        [DisplayName("ApplicationPaymentStatusRequest:SiteNumber")]
        public string? ApplicantName { get; set; }

        public PaymentRequestStatus Status { get; set; }

        public bool isPermitted { get; set; }

        public List<string> ErrorList { get; set; } = new List<string> { };

        public bool IsL3ApprovalRequired { get; set; }

        public PaymentRequestStatus ToStatus { get; set; }

        public string StatusText { get; set; } = string.Empty;

        public string ToStatusText { get; set; } = string.Empty;

    }
}
