using System;
using Unity.Payments.Enums;
using Volo.Abp.Application.Dtos;

namespace Unity.Payments.PaymentRequests;

[Serializable]
public class ExpenseApprovalDto : AuditedEntityDto<Guid>
{
    public ExpenseApprovalType Type { get; set; }
    public ExpenseApprovalStatus Status { get; set; }

    public DateTime? DecisionDate { get; set; }
    public Guid? DecisionUserId { get; set; }

    public PaymentUserDto? DecisionUser { get; set; }
}
