using System;
using Unity.Modules.Shared.MessageBrokers.RabbitMQ.Interfaces;

namespace Unity.Payments.RabbitMQ.QueueMessages
{
    public class ReconcilePaymentMessages : IQueueMessage
    {
        public Guid MessageId { get; set; }
        public TimeSpan TimeToLive { get; set; }
        public Guid PaymentRequestId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public string SupplierNumber { get; set; } = string.Empty;
        public string SiteNumber { get; set; } = string.Empty;
        public Guid TenantId { get; set; }
    }
}
