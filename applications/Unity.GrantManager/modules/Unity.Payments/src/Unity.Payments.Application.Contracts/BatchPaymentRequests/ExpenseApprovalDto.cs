using System;
using Unity.Payments.Enums;
using Volo.Abp.Application.Dtos;

namespace Unity.Payments.BatchPaymentRequests
{
    [Serializable]
    public class ExpenseApprovalDto : AuditedEntityDto<Guid>
    {
        public ExpenseApprovalTypeDto Type { get; set; }
        public ExpenseApprovalStatusDto Status { get; set; }
    }
}
