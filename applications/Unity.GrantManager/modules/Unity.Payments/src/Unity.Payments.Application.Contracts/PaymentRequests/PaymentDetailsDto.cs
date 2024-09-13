using System;
using System.Collections.ObjectModel;
using Unity.Payments.Enums;
using Volo.Abp.Application.Dtos;

namespace Unity.Payments.PaymentRequests
{
    [Serializable]
    public class PaymentDetailsDto : AuditedEntityDto<Guid>
    {
        public string InvoiceNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public PaymentRequestStatus Status { get; set; }
        public string? Description { get; set; }
        public bool IsRecon { get; set; }
        public string? InvoiceStatus { get; set; }
        public string? PaymentStatus { get; set; }
        public string? PaymentNumber { get; set; }
        public string? PaymentDate { get; set; }
        public Guid CorrelationId { get; set; }
        public string PayeeName { get; set; } = string.Empty;
        public string ContractNumber { get; set; } = string.Empty;
        public string SupplierNumber { get; set; } = string.Empty;
        public string CorrelationProvider { get; set; } = string.Empty;
        public string? CasResponse { get; set; }
        public string ReferenceNumber { get; set; }
        public Collection<ExpenseApprovalDto> ExpenseApprovals { get; set; } = [];
    }
}
