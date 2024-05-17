using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Unity.Payments.Web.Pages.Payments
{
    public class PaymentsModel
    {
        [DisplayName("ApplicationPaymentRequest:Amount")]
        [Required]
        public decimal Amount { get; set; }
        [DisplayName("ApplicationPaymentRequest:Description")]
        public string? Description { get; set; }
        [Required(ErrorMessage = "This field is required.")]
        [DisplayName("ApplicationPaymentRequest:InvoiceNumber")]
        public string InvoiceNumber { get; set; } = string.Empty;
        public Guid CorrelationId { get; set; }
        [Required(ErrorMessage = "This field is required.")]
        [DisplayName("ApplicationPaymentRequest:SiteNumber")]
        public Guid SiteId { get; set; }
        public bool? Payable { get; set; }
        public string? ApplicantName { get; set; }
        public decimal PaymentThreshold { get; set; }
        public List<SelectListItem> SiteList { get; set; } = new List<SelectListItem>{};
        public List<string> ErrorList { get; set; } = new List<string>{};
        public bool DisableFields  { get; set; } = false;
        public string? ContractNumber { get; set; }
        public string? SupplierNumber { get; set; }
        public decimal RemainingAmount { get; set; }
    }
}
