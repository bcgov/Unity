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
        
        /* Address */
        public virtual string? MailingAddress { get; private set; }
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
            bool isFin312,
            string? mailingAddress = default,
            string? city = default,
            string? province = default,
            string? postalCode = default)
           : base(id)
        {
            Number = number;
            PaymentMethod = paymentMethod;
            IsFin312 = isFin312;
            MailingAddress = mailingAddress;
            City = city;
            Province = province;
            PostalCode = postalCode;
        }
    }
}
