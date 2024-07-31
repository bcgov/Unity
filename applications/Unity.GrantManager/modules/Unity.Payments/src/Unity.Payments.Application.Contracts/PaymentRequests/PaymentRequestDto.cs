using System;
using System.Collections.ObjectModel;
using Unity.Payments.Enums;
using Unity.Payments.Suppliers;
using Volo.Abp.Application.Dtos;

namespace Unity.Payments.PaymentRequests
{
#pragma warning disable CS8618
    [Serializable]
    public class PaymentRequestDto : AuditedEntityDto<Guid>
    {
        public string InvoiceNumber { get; set; }
        public decimal Amount { get; set; }
        public PaymentRequestStatus Status { get; set; }
        public string? Description { get; set; }
        public bool IsRecon { get; set; }
        public string? InvoiceStatus { get; set; }
        public string? PaymentStatus { get; set; }
        public string? PaymentNumber { get; set; }
        public string? PaymentDate { get; set; }
        public Guid CorrelationId { get; set; }
        public SiteDto? Site { get; set; }
        public string PayeeName { get; set; }
        public string ContractNumber { get; set; }
        public string SupplierNumber { get; set; }
        public  string CorrelationProvider { get;  set; } = string.Empty;
        public string? CasResponse { get; set; }
        public string ReferenceNumber { get; set; }

        public  Collection<ExpenseApprovalDto> ExpenseApprovals { get;  set; }

    }
#pragma warning restore CS8618
}
