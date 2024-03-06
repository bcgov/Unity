using System;
using Unity.Payments.Enums;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.Payments
{
    public class PaymentRequest : FullAuditedEntity<Guid>, IMultiTenant
    {
        public Guid? TenantId { get; set; }
        public virtual string InvoiceNumber { get; private set; } = string.Empty;
        public virtual decimal Amount { get; private set; }
        public virtual PaymentMethod Method { get; private set; }
        public virtual PaymentStatus Status { get; private set; } = PaymentStatus.Created;
        public virtual string? Comment { get; private set; } = null;

        /// <summary>
        /// The external system / module Id that this relates to
        /// </summary>
        public virtual Guid CorrelationId { get; private set; }

        public virtual PaymentRequestBatch? Batch { get; private set; }
        public bool IsRecon { get; internal set; }

        protected PaymentRequest()
        {
            /* This constructor is for ORMs to be used while getting the entity from the database. */
        }

        public PaymentRequest(Guid id,
            PaymentRequestBatch batch,
            decimal amount,
            PaymentMethod method,
            Guid correlationId,
            string? comment = null)
            : base(id)
        {
            Amount = amount;
            Method = method;
            Comment = comment;
            Batch = batch;
            CorrelationId = correlationId;
        }

        public PaymentRequest SetAmount(decimal amount)
        {
            Amount = amount;
            return this;
        }

        public PaymentRequest SetPaymentMethod(PaymentMethod method)
        {
            Method = method;
            return this;
        }

        public PaymentRequest SetComment(string comment)
        {
            Comment = comment;
            return this;
        }

        public PaymentRequest SetStatus(PaymentStatus status)
        {
            Status = status;
            return this;
        }
    }
}
