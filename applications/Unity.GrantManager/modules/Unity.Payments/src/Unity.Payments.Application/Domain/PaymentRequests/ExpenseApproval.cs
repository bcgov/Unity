using System;
using Unity.Payments.Enums;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.Payments.Domain.PaymentRequests;

public class ExpenseApproval : FullAuditedEntity<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }
    public virtual ExpenseApprovalType Type { get; private set; } = ExpenseApprovalType.Level1;
    public virtual ExpenseApprovalStatus Status { get; private set; } = ExpenseApprovalStatus.Requested;

    public virtual Guid PaymentRequestId { get; set; }

    private Guid? _decisionUserId;
    public virtual Guid? DecisionUserId
    {
        get => _decisionUserId;
        set
        {
            if (_decisionUserId != null)
            {
                throw new InvalidOperationException("DecisionUserId cannot be changed once it is set.");
            }
            _decisionUserId = value;
        }
    }

    private DateTime? _decisionDate;
    public virtual DateTime? DecisionDate
    {
        get => _decisionDate;
        set
        {
            if (_decisionDate != null)
            {
                throw new InvalidOperationException("DecisionDate cannot be changed once it is set.");
            }
            _decisionDate = value;
        }
    }

    public virtual PaymentRequest PaymentRequest
    {
        set => _paymentRequest = value;
        get => _paymentRequest
               ?? throw new InvalidOperationException("Uninitialized property: " + nameof(PaymentRequest));
    }
    private PaymentRequest? _paymentRequest;

    public ExpenseApproval()
    {
        /* This constructor is for ORMs to be used while getting the entity from the database. */
    }

    public ExpenseApproval(Guid id,
        ExpenseApprovalType type)
        : base(id)
    {
        Type = type;
    }

    public ExpenseApproval Approve(Guid currentUserId)
    {
        Status = ExpenseApprovalStatus.Approved;
        DecisionUserId = currentUserId;
        DecisionDate = DateTime.UtcNow;
        return this;
    }

    public ExpenseApproval Decline(Guid currentUserId)
    {
        Status = ExpenseApprovalStatus.Declined;
        DecisionUserId = currentUserId;
        DecisionDate = DateTime.UtcNow;
        return this;
    }
}
