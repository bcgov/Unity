using System;
using Unity.Payments.Enums;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.Payments.Domain.PaymentRequests
{
    public class ExpenseApproval : FullAuditedEntity<Guid>, IMultiTenant
    {
        public Guid? TenantId { get; set; }
        public virtual ExpenseApprovalType Type { get; private set; } = ExpenseApprovalType.Level1;
        public virtual ExpenseApprovalStatus Status { get; private set; } = ExpenseApprovalStatus.Requested;
      
        public virtual Guid PaymentRequestId { get; set; }

        public virtual DateTime? DecisionDate { get; set; }

        public virtual PaymentRequest PaymentRequest
        {
            set => _paymentRequest = value;
            get => _paymentRequest
                   ?? throw new InvalidOperationException("Uninitialized property: " + nameof(PaymentRequest));
        }
        private PaymentRequest? _paymentRequest;

        protected ExpenseApproval()
        {
            /* This constructor is for ORMs to be used while getting the entity from the database. */
        }

        public ExpenseApproval(Guid id,
            ExpenseApprovalType type)
            : base(id)
        {
            Type = type;
        }

        public ExpenseApproval Approve()
        {
            Status = ExpenseApprovalStatus.Approved;
            DecisionDate = DateTime.UtcNow;
            return this;
        }

        public ExpenseApproval Decline()
        {

            Status = ExpenseApprovalStatus.Declined;
            DecisionDate = DateTime.UtcNow;
            return this;
        }
    }
}
