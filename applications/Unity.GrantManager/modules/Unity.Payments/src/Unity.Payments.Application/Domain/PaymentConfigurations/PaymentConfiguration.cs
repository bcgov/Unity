using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.Payments.Domain.PaymentConfigurations
{
    public class PaymentConfiguration : FullAuditedAggregateRoot<Guid>, IMultiTenant
    {
        public Guid? TenantId { get; set; }
        public Guid? DeafaultAccountCodingId { get; set; }
        public string PaymentIdPrefix { get; set; } = string.Empty;
        public decimal? PaymentThreshold { get; set; }

        protected PaymentConfiguration()
        {
            /* This constructor is for ORMs to be used while getting the entity from the database. */
        }

        public PaymentConfiguration(
            decimal? paymentThreshold,
            string paymentIdPrefix)
        {
            PaymentThreshold = paymentThreshold;
            PaymentIdPrefix = paymentIdPrefix;            
        }
    }
}
