﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Unity.Payments.Web.Pages.PaymentApprovals
{
    public class PaymentsApprovalModel
    {
        [DisplayName("ApplicationPaymentStatusRequest:Id")]
        [Required]
        public Guid Id { get; set; }
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
    }
}