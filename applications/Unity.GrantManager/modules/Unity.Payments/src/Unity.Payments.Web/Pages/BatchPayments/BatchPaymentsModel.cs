using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System;

namespace Unity.Payments.Web.Pages.BatchPayments
{
    public class BatchPaymentsModel
    {
        [DisplayName("ApplicationPaymentRequest:Amount")]
        [Required]
        public decimal Amount { get; set; }
        [DisplayName("ApplicationPaymentRequest:Description")]
        public string? Description { get; set; }
        [Required(ErrorMessage = "This field is required.")]
        [DisplayName("ApplicationPaymentRequest:InvoiceNumber")]
        public string InvoiceNumber { get; set; } = string.Empty;
        public Guid ApplicationId { get; set; }
        public bool? Payable { get; set; }
        public string? ApplicantName { get; set; }
        public decimal PaymentThreshold { get; set; }
    }
}
