using System;
using Unity.Payments.Enums;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.Payments.Suppliers
{
    public class Site : FullAuditedAggregateRoot<Guid>, IMultiTenant
    {
        public Guid? TenantId { get; set; }
        public virtual string SiteNumber { get; private set; } = string.Empty;
        public virtual PaymentGroup PaymentMethod { get; private set; }
        public virtual bool IsFin312 { get; private set; }
        public virtual string PhysicalAddress { get; private set; } = string.Empty;

        protected Site()
        {
            /* This constructor is for ORMs to be used while getting the entity from the database. */
        }

        public Site(Guid id,
            string siteNumber,
            PaymentGroup paymentMethod,
            bool isFin312,
            string physicalAddress)
           : base(id)
        {
            SiteNumber = siteNumber;
            PaymentMethod = paymentMethod;
            IsFin312 = isFin312;
            PhysicalAddress = physicalAddress;
        }
    }
}
