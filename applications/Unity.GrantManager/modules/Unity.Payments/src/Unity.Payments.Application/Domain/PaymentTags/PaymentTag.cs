using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.Payments.Domain.PaymentTags
{
    public class PaymentTag : AuditedAggregateRoot<Guid>, IMultiTenant
    {
        public Guid? TenantId { get; set; }
        public Guid PaymentRequestId { get; set; }
        public string Text { get; set; } = string.Empty;

        protected PaymentTag()
        {
            /* This constructor is for ORMs to be used while getting the entity from the database. */
        }

        public PaymentTag(Guid id,
            Guid paymentRequestId,
            string text)
           : base(id)
        {
            PaymentRequestId = paymentRequestId;
            Text = text;
        }

    }
}
