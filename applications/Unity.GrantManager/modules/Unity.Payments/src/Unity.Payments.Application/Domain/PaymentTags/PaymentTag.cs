using System;
using Unity.GrantManager.GlobalTag;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.Payments.Domain.PaymentTags
{
    public class PaymentTag : AuditedAggregateRoot<Guid>, IMultiTenant
    {
        public Guid? TenantId { get; set; }
        public Guid PaymentRequestId { get; set; }
        public string Text { get; set; } = string.Empty;

        public Guid TagId { get; set; }

        public virtual Tag Tag
        {
            set => _tag = value;
            get => _tag
                   ?? throw new InvalidOperationException("Uninitialized property: " + nameof(Tag));
        }
        private Tag? _tag;

        public PaymentTag()
        {
            /* This constructor is for ORMs to be used while getting the entity from the database. */
        }

        public PaymentTag(Guid id,
            Guid paymentRequestId,
            Guid tagId)
           : base(id)
        {
            PaymentRequestId = paymentRequestId;
            TagId = tagId;
        }

    }
}
