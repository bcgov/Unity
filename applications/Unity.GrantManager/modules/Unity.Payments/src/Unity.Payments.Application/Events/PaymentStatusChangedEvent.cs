using System;
using Unity.Payments.Enums;

namespace Unity.Payments.Events
{
    public class PaymentStatusChangedEvent
    {
        public Guid PaymentRequestId { get; set; }

        public Guid ApplicationId { get; set; }

        public PaymentRequestStatus Status { get; set; }

        // Null when the payment is first created.
        public PaymentRequestStatus? PreviousStatus { get; set; }

        public string? CasPaymentStatus { get; set; }

        public string? PreviousCasPaymentStatus { get; set; }

        public Guid? TenantId { get; set; }
    }
}