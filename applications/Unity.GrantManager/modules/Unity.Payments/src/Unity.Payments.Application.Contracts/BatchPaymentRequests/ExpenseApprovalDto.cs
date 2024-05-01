using System;
using Unity.Payments.Enums;
using Volo.Abp.Application.Dtos;

namespace Unity.Payments.BatchPaymentRequests
{
    [Serializable]
    public class ExpenseApprovalDto : AuditedEntityDto<Guid>
    {
        public ExpenseApprovalType Type { get; set; }
        public ExpenseApprovalStatus Status { get; set; }
    }
}
