﻿using System;

namespace Unity.Payments.PaymentRequests
{
#pragma warning disable CS8618
    [Serializable]
    public class CreatePaymentRequestDto
    {
        public string InvoiceNumber { get; set; }
        public decimal Amount { get; set; }
        public string? Description { get; set; }
        public Guid CorrelationId { get; set; }
        public Guid SiteId { get; set; }
        public string PayeeName { get; set; }
        public string ContractNumber { get; set; }
        public string SupplierNumber { get; set; }
        public string SupplierName { get; set; }
        public string CorrelationProvider { get; set; } = string.Empty;
        public string BatchName { get; set; }
        public decimal BatchNumber { get; set; } = 0;
        public string ReferenceNumber { get; set; } = string.Empty;
        public string SubmissionConfirmationCode { get; set; } = string.Empty;
        public string? InvoiceStatus { get; set; }
        public string? PaymentStatus { get; set; }
        public string? PaymentNumber { get; set; }
        public string? PaymentDate { get; set; }
        public Guid? AccountCodingId { get; set; }
        public string? Note { get; set; }
    }

    public class UpdatePaymentStatusRequestDto
    {
        public Guid PaymentRequestId { get; set; }
        public bool IsApprove { get; set; }
        public string Note { get; set; }
    }
#pragma warning restore CS8618
}

