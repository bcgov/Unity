using System;
using Unity.Payments.Enums;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.Payments.BatchPaymentRequests
{
    public class ExpenseApproval : FullAuditedEntity<Guid>, IMultiTenant
    {
        public Guid? TenantId { get; set; }
        public virtual ExpenseApprovalType Type { get; private set; } = ExpenseApprovalType.Level1;
        public virtual ExpenseApprovalStatus Status { get; private set; } = ExpenseApprovalStatus.Requested;
        public virtual BatchPaymentRequest? BatchPaymentRequest { get; set; }
        public virtual Guid BatchPaymentRequestId { get; set; }

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
            return this;
        }

        public ExpenseApproval Decline()
        {

            Status = ExpenseApprovalStatus.Declined;
            return this;
        }
    }
}
