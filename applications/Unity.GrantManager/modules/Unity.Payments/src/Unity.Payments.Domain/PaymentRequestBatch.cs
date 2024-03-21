using System;
using System.Collections.ObjectModel;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;
using Volo.Abp;
using System.Linq;
using Unity.Payments.Enums;
using Unity.Payments.Correlation;

namespace Unity.Payments
{
    public class BatchPaymentRequest : FullAuditedAggregateRoot<Guid>, IMultiTenant, ICorrelationProviderEntity
    {
        public Guid? TenantId { get; set; }
        public virtual string BatchNumber { get; private set; } = string.Empty;
        public virtual string ExpenseAuthorityName { get; private set; } = string.Empty;
        public virtual string IssuedByName { get; private set; } = string.Empty;
        public virtual PaymentMethod Method { get; private set; }
        public virtual PaymentRequestStatus Status { get; private set; } = PaymentRequestStatus.Created;
        public virtual bool IsApproved { get => Approvals.All(s => s.Status == ExpenseApprovalStatus.Approved); }
        public virtual bool IsRecon { get => PaymentRequests.All(s => s.IsRecon); }       
        public virtual string? Description { get; private set; }
        public virtual Collection<PaymentRequest> PaymentRequests { get; private set; }
        public virtual Collection<ExpenseApproval> Approvals { get; private set; }

        // External Correlation
        public virtual string CorrelationProvider { get; private set; } = string.Empty;

        protected BatchPaymentRequest()
        {
            /* This constructor is for ORMs to be used while getting the entity from the database. */
            Approvals = new Collection<ExpenseApproval>();
            PaymentRequests = new Collection<PaymentRequest>();
        }

        public BatchPaymentRequest(Guid id,
            string batchNumber,
            PaymentMethod paymentMethod,
            string? description,
            string correlationProvider)
           : base(id)
        {
            BatchNumber = batchNumber;
            Method = paymentMethod;
            Description = description;
            CorrelationProvider = correlationProvider;
            Approvals = GenerateDefaultExpenseApprovals();
            PaymentRequests = new Collection<PaymentRequest>();
        }        

        public static Collection<ExpenseApproval> GenerateDefaultExpenseApprovals()
        {
            return new Collection<ExpenseApproval>()
            {
                new ExpenseApproval(Guid.NewGuid(), ExpenseApprovalType.Level1),
                new ExpenseApproval(Guid.NewGuid(), ExpenseApprovalType.Level2)
            };
        }

        public BatchPaymentRequest GenerateBatchNumber()
        {
            if (BatchNumber != string.Empty) return this;

            BatchNumber = Guid.NewGuid().ToString();
            return this;
        } 

        public BatchPaymentRequest AddPaymentRequest(PaymentRequest paymentRequest)
        {
            decimal paymentThreshold = 500000; // This value will be tenant specific!        

            if (paymentRequest.Amount >= paymentThreshold
                && PaymentRequests.Count > 0)
            {
                throw new BusinessException(message: $"Cannot add a payment to an existing batch with a value equal or above threshold {paymentThreshold}");
            }

            if (PaymentRequests.Count == 1
                && PaymentRequests[0].Amount >= paymentThreshold)
            {
                throw new BusinessException(message: $"Cannot add a payment to existing batch that already has a payment equal or above the threshold {paymentThreshold}");
            }            

            if (paymentRequest.Amount >= paymentThreshold)
            {
                Approvals.Add(new ExpenseApproval(Guid.NewGuid(), ExpenseApprovalType.Level3));
            }

            PaymentRequests.Add(paymentRequest);
            return this;
        }       
    }
}
