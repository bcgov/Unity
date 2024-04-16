using System;
using Unity.Payments.Enums;
using Volo.Abp.Application.Dtos;

namespace Unity.Payments.BatchPaymentRequests
{
#pragma warning disable CS8618
    [Serializable]
    public class PaymentRequestDto : AuditedEntityDto<Guid>
    {
        public string InvoiceNumber { get; set; }
        public decimal Amount { get; set; }
        public PaymentRequestStatusDto Status { get; set; }
        public string? Description { get; set; }
        public bool IsRecon { get; set; }
        public string? InvoiceStatus { get; set; }
        public string? PaymentStatus { get; set; }
        public string? PaymentNumber { get; set; }
        public string? PaymentDate { get; set; }
        public Guid CorrelationId { get; set; }
        public Guid SiteId { get; set; }
    }
#pragma warning restore CS8618
}
