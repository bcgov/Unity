using System;

namespace Unity.Payments.PaymentRequests
{
#pragma warning disable CS8618
    [Serializable]
    public class CreateHistoricalPaymentRequestDto
    {
        public string InvoiceNumber { get; set; }
        public decimal Amount { get; set; }
        public string? Description { get; set; }
        public Guid CorrelationId { get; set; }
        public Guid? SiteId { get; set; }
        public string PayeeName { get; set; }
        public string ContractNumber { get; set; }
        public string? SupplierNumber { get; set; }
        public string? SupplierName { get; set; }
        public string CorrelationProvider { get; set; } = string.Empty;
        public string BatchName { get; set; }
        public decimal BatchNumber { get; set; } = 0;
        public string ReferenceNumber { get; set; } = string.Empty;
        public string SubmissionConfirmationCode { get; set; } = string.Empty;
        public Guid? AccountCodingId { get; set; }
        public string? Note { get; set; }
        public string PaidDate { get; set; }
    }
#pragma warning restore CS8618
}
