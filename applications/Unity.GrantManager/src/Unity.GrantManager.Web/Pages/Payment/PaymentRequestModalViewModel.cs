using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Unity.GrantManager.Web.Pages.Payment
{
    public class ApplicationPaymentRequestModalViewModel
    {
        [DisplayName("ApplicationPaymentRequest:Amount")]
        [RegularExpression(@"^\d*\.?\d+$", ErrorMessage = "Please enter a value greater than $0.00.")]
        [Required]
        public decimal Amount { get; set; }
        [DisplayName("ApplicationPaymentRequest:Description")]
        public string? Description { get; set; }
        [Required]
        [DisplayName("ApplicationPaymentRequest:InvoiceNumber")]
        public string InvoiceNumber { get; set; } = string.Empty;
        public Guid ApplicationId { get; set; }
        public bool? Payable { get; set; }
        public string? ApplicantName { get; set; }
    }
}
