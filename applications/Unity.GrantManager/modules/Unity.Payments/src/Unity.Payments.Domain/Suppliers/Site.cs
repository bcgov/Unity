using System;
using Unity.Payments.Enums;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.Payments.Suppliers
{
    public class Site : FullAuditedEntity<Guid>, IMultiTenant
    {
        public Guid? TenantId { get; set; }
        public virtual uint Number { get; set; }
        public virtual PaymentGroup PaymentMethod { get; set; }
        
        /* Address */
        public virtual string? AddressLine1 { get; set; }
        public virtual string? AddressLine2 { get; set; }
        public virtual string? AddressLine3 { get; set; }
        public virtual string? City { get; set; }
        public virtual string? Province { get; set; }
        public virtual string? PostalCode { get; set; }

        /* Supplier */
        public virtual Supplier? Supplier { get; set; }
        public virtual Guid SupplierId { get; set; }

        public Site()
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
