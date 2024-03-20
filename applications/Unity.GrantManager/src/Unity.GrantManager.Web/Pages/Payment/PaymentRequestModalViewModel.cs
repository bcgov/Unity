using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Unity.GrantManager.Web.Pages.Payment
{
    public class ApplicationPaymentRequestModalViewModel
    {

        [DisplayName("ApplicationPaymentRequest:Amount")]
        [Required]
        public decimal? Amount { get; set; }

        [DisplayName("ApplicationPaymentRequest:Description")]
        public string? Description { get; set; }

        [DisplayName("ApplicationPaymentRequest:InvoiceNumber")]
        public string? InvoiceNumber { get; set; }

        public Guid ApplicationId { get; set; }

        public bool? Payable { get; set; }

        public string? ApplicantName { get; set; }

    }

}
