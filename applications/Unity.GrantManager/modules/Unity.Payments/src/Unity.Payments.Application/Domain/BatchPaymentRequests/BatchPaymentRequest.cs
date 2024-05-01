using System;
using System.Collections.ObjectModel;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;
using Volo.Abp;
using System.Linq;
using Unity.Modules.Shared.Correlation;
using Unity.Payments.Domain.Exceptions;
using Unity.Payments.Enums;

namespace Unity.Payments.Domain.BatchPaymentRequests
{
    public class BatchPaymentRequest : FullAuditedAggregateRoot<Guid>, IMultiTenant, ICorrelationProviderEntity
    {
        public Guid? TenantId { get; set; }
        public virtual string BatchNumber { get; private set; } = string.Empty;
        public virtual string RequesterName { get; private set; } = string.Empty;
        public virtual PaymentRequestStatus Status { get; private set; } = PaymentRequestStatus.Created;
        public virtual bool IsApproved { get => ExpenseApprovals.All(s => s.Status == ExpenseApprovalStatus.Approved); }
        public virtual bool IsRecon { get => PaymentRequests.All(s => s.IsRecon); }
        public virtual string? Description { get; private set; }

        // Optional for now, still to be enforced in upcoming work
        public virtual Guid? SiteId { get; set; }

        public virtual Collection<PaymentRequest> PaymentRequests { get; private set; }
        public virtual Collection<ExpenseApproval> ExpenseApprovals { get; private set; }

        // External Correlation
        public virtual string CorrelationProvider { get; private set; } = string.Empty;

        protected BatchPaymentRequest()
        {
            /* This constructor is for ORMs to be used while getting the entity from the database. */
            ExpenseApprovals = [];
            PaymentRequests = [];
        }

        public BatchPaymentRequest(Guid id,
            string batchNumber,
            string? description,
            string requesterName,
            string correlationProvider)
           : base(id)
        {
            BatchNumber = batchNumber;
            Description = description;
            RequesterName = requesterName;
            CorrelationProvider = correlationProvider;
            ExpenseApprovals = GenerateDefaultExpenseApprovals();
            PaymentRequests = [];
        }

        public static Collection<ExpenseApproval> GenerateDefaultExpenseApprovals() => [
                new ExpenseApproval(Guid.NewGuid(), ExpenseApprovalType.Level1),
                new ExpenseApproval(Guid.NewGuid(), ExpenseApprovalType.Level2)
            ];

        public BatchPaymentRequest GenerateBatchNumber()
        {
            if (BatchNumber != string.Empty) return this;

            BatchNumber = Guid.NewGuid().ToString();
            return this;
        }

        public BatchPaymentRequest AddPaymentRequest(PaymentRequest paymentRequest, decimal paymentThreshold = 500000m)
        {
            if (paymentRequest.Amount >= paymentThreshold
                && PaymentRequests.Count > 0)
            {
                throw new BusinessException(ErrorConsts.ThesholdExceeded).WithData("Threshold", paymentThreshold.ToString("0.00"));
            }

            if (PaymentRequests.Count == 1
                && PaymentRequests[0].Amount >= paymentThreshold)
            {
                throw new BusinessException(ErrorConsts.ThesholdExceeded).WithData("Threshold", paymentThreshold.ToString("0.00"));
            }

            if (paymentRequest.Amount <= 0)
            {
                throw new BusinessException(ErrorConsts.ZeroPayment);
            }

            if (paymentRequest.Amount >= paymentThreshold)
            {
                ExpenseApprovals.Add(new ExpenseApproval(Guid.NewGuid(), ExpenseApprovalType.Level3));
            }


            PaymentRequests.Add(paymentRequest);
            return this;
        }
    }
}
