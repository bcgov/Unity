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
        
        /* Address */
        public virtual string? AddressLine1 { get; private set; }
        public virtual string? AddressLine2 { get; private set; }
        public virtual string? AddressLine3 { get; private set; }
        public virtual string? City { get; private set; }
        public virtual string? Province { get; private set; }
        public virtual string? PostalCode { get; private set; }

        /* Supplier */
        public virtual Supplier? Supplier { get; set; }
        public virtual Guid SupplierId { get; set; }

        protected Site()
        {
            /* This constructor is for ORMs to be used while getting the entity from the database. */
        }

        public Site(Guid id,
            uint number,
            PaymentGroup paymentMethod,
            string? addressLine1 = default,
            string? addressLine2 = default,
            string? addressLine3 = default,
            string? city = default,
            string? province = default,
            string? postalCode = default)
           : base(id)
        {
            Number = number;
            PaymentMethod = paymentMethod;
            AddressLine1 = addressLine1;
            AddressLine2 = addressLine2;
            AddressLine3 = addressLine3;
            City = city;
            Province = province;
            PostalCode = postalCode;
        }
    }
}
