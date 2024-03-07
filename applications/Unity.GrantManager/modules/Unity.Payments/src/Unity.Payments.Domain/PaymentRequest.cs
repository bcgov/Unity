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
        public virtual PaymentRequestStatus Status { get; private set; } = PaymentRequestStatus.Created;
        public virtual string? Comment { get; private set; } = null;

        /// <summary>
        /// The external system / module Id that this relates to
        /// </summary>
        public virtual Guid CorrelationId { get; private set; }

        public virtual PaymentRequestBatch? Batch { get; private set; }
        public bool IsRecon { get; internal set; }

        // Filled on a recon
        public virtual string? InvoiceStatus { get; private set; }
        public virtual string? PaymentStatus { get; private set; }
        public virtual string? PaymentNumber { get; private set; }
        public virtual string? PaymentDate { get; private set; }

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

        public PaymentRequest SetPaymentRequestStatus(PaymentRequestStatus status)
        {
            Status = status;
            return this;
        }

        public PaymentRequest SetInvoiceStatus(string status)
        {
            InvoiceStatus = status;
            return this;
        }

        public PaymentRequest SetPaymentStatus(string status)
        {
            PaymentStatus = status;
            return this;
        }

        public PaymentRequest SetPaymentNumber(string paymentNumber)
        {
            PaymentNumber = paymentNumber;
            return this;
        }

        public PaymentRequest SetPaymentDate(string paymentDate)
        {
            PaymentDate = paymentDate;
            return this;
        }     
    }
}
