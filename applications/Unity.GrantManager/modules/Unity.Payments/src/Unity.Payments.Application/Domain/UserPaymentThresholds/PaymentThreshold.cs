using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.Payments.Domain.PaymentThresholds
{
    public class PaymentThreshold : FullAuditedAggregateRoot<Guid>, IMultiTenant
    {
        public Guid? TenantId { get; set; }
        public Guid? UserId { get; set; }        
        public decimal? Threshold { get; set; }
        public string? Description { get; set; }

        public PaymentThreshold()
        {
            /* This constructor is for ORMs to be used while getting the entity from the database. */
        }

    }
}
