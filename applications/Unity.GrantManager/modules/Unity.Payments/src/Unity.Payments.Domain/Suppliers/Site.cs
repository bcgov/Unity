using System;
using Unity.Payments.Enums;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.Payments.Suppliers
{
    public class Site : FullAuditedAggregateRoot<Guid>, IMultiTenant
    {
        public Guid? TenantId { get; set; }
        public virtual uint Number { get; private set; }
        public virtual PaymentGroup PaymentMethod { get; private set; }
        public virtual bool IsFin312 { get; private set; }
        public virtual string PhysicalAddress { get; private set; } = string.Empty;
        public virtual Supplier? Supplier { get; set; }
        public virtual Guid SupplierId { get; set; }

        protected Site()
        {
            /* This constructor is for ORMs to be used while getting the entity from the database. */
        }

        public Site(Guid id,
            uint number,
            PaymentGroup paymentMethod,
            bool isFin312,
            string physicalAddress)
           : base(id)
        {
            Number = number;
            PaymentMethod = paymentMethod;
            IsFin312 = isFin312;
            PhysicalAddress = physicalAddress;
        }
    }
}
