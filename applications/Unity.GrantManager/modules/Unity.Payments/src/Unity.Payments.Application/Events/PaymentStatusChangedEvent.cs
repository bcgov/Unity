using System;
using Unity.Payments.Enums;

namespace Unity.Payments.Events
{
    public class PaymentStatusChangedEvent
    {
        public Guid PaymentRequestId { get; set; }

        public Guid ApplicationId { get; set; }

        public PaymentRequestStatus Status { get; set; }

        public Guid? TenantId { get; set; }
    }
}