using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.Payments
{
    public class Payment : AuditedAggregateRoot<Guid>, IMultiTenant
    {
        public virtual decimal Amount { get; private set; }
        public virtual PaymentMethod Method { get; private set; }
        public virtual PaymentStatus Status { get; private set; } = PaymentStatus.Created;
        public virtual string? Comment { get; private set; } = null;
        public virtual Collection<PaymentProperty> Properties { get; private set; } = new Collection<PaymentProperty>();
        public virtual Collection<PaymentApproval> Approvals { get; private set; } = new Collection<PaymentApproval>();

        protected Payment()
        {
            /* This constructor is for ORMs to be used while getting the entity from the database. */
        }

        public Payment(Guid id,
            decimal amount,
            PaymentMethod paymentMethod,
            string? comment = null)
            : base(id)
        {
            Amount = amount;
            Method = paymentMethod;
            Comment = comment;

            Properties = new Collection<PaymentProperty>();
            Approvals = new Collection<PaymentApproval>();
        }

        public Payment SetAmount(decimal amount)
        {
            Amount = amount;
            return this;
        }

        public Payment SetPaymentMethod(PaymentMethod paymentMethod)
        {
            Method = paymentMethod;
            return this;
        }

        public Payment SetComment(string comment)
        {
            Comment = comment;
            return this;
        }

        public Payment AddProperty(string key, string value)
        {
            Properties.Add(new PaymentProperty(Guid.NewGuid(), key, value));
            return this;
        }

        public Payment AddApproval()
        {
            Approvals.Add(new PaymentApproval(Guid.NewGuid(), Approvals.Max(s => s.Level) + 1));
            return this;
        }

        public Payment SetStatus(PaymentStatus status)
        {
            Status = status;
            return this;
        }

        public Guid? TenantId { get; set; }
    }

    public class PaymentProperty : AuditedEntity<Guid>, IMultiTenant
    {
        public virtual string? Key { get; private set; }

        public virtual string? Value { get; private set; }

        protected PaymentProperty()
        {
            /* This constructor is for ORMs to be used while getting the entity from the database. */
        }

        public PaymentProperty(Guid id,
            string key,
            string value)
            : base(id)
        {
            Key = key.ToUpper().Trim();
            Value = value;
        }

        public Guid? TenantId { get; set; }
    }

    public class PaymentApproval : AuditedEntity<Guid>, IMultiTenant
    {
        public uint Level { get; private set; } = 1;
        public PaymentApprovalStatus Status { get; private set; } = PaymentApprovalStatus.Requested;

        protected PaymentApproval()
        {
            /* This constructor is for ORMs to be used while getting the entity from the database. */
        }

        public PaymentApproval(Guid id,
            uint level)
            : base(id)
        {
            Level = level;
        }

        public Guid? TenantId { get; set; }
    }

    public enum PaymentApprovalStatus
    {
        Requested,
        Approved,
        Declined
    }

    public enum PaymentStatus
    {
        Created
    }

    public enum PaymentMethod
    {
        EFT,
        Cheque
    }
}
