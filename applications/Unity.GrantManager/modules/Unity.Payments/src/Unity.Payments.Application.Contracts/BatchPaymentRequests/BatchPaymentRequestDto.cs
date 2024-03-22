using System;
using System.Collections.ObjectModel;
using Unity.Payments.Enums;
using Volo.Abp.Application.Dtos;

namespace Unity.Payments.BatchPaymentRequests
{
#pragma warning disable CS8618
    [Serializable]
    public class BatchPaymentRequestDto : ExtensibleFullAuditedEntityDto<Guid>
    {        
        public string BatchNumber { get; set; }
        public string ExpenseAuthorityName { get; set; }
        public string IssuedByName { get; set; }
        public PaymentGroupDto PaymentGroup { get; set; }
        public PaymentRequestStatusDto Status { get; set; }
        public bool IsApproved { get; set; }
        public bool IsRecon { get; set; }
        public string? Description { get; set; }                
        public string CorrelationProvider { get; set; }
        public Collection<PaymentRequestDto> PaymentRequests { get; set; }
        public Collection<ExpenseApprovalDto> Approvals { get; private set; }
    }
#pragma warning restore CS8618
}
