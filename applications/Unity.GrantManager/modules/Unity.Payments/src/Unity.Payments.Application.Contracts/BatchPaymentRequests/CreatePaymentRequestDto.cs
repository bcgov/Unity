using System;

namespace Unity.Payments.BatchPaymentRequests
{
#pragma warning disable CS8618
    [Serializable]
    public class CreatePaymentRequestDto
    {
        public string InvoiceNumber { get; set; }
        public decimal Amount { get; set; }                
        public string? Description { get; set; }        
        public Guid CorrelationId { get; set; }
    }
#pragma warning restore CS8618
}

