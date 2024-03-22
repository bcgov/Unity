using System;
using Unity.Payments.Enums;
using Volo.Abp.Application.Dtos;

namespace Unity.Payments.BatchPaymentRequests
{
#pragma warning disable CS8618
    [Serializable]
    public class ExpenseApprovalDto : AuditedEntityDto<Guid>
    {
        public ExpenseApprovalTypeDto Type { get; set; }
        public ExpenseApprovalStatusDto Status { get; set; }
    }
#pragma warning restore CS8618
}
