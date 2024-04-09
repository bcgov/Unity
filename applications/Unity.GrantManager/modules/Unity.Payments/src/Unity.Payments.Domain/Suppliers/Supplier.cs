using System;
using System.Collections.ObjectModel;
using Unity.Payments.Correlation;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.Payments.Suppliers
{
    public class Supplier : FullAuditedEntity<Guid>, IMultiTenant, ICorrelationEntity
    {
        public Guid? TenantId { get; set; }
        public virtual string Name { get; set; } = string.Empty;
        public virtual uint Number { get; set; }
        public virtual Collection<Site> Sites { get; private set; }

        /* Address */
        public virtual string? MailingAddress { get; private set; }
        public virtual string? City { get; private set; }
        public virtual string? Province { get; private set; }
        public virtual string? PostalCode { get; private set; }

        // External Correlation
        public virtual string CorrelationProvider { get; set; } = string.Empty;
        public virtual Guid CorrelationId { get; set; }

        public Supplier()
        {
            /* This constructor is for ORMs to be used while getting the entity from the database. */
            Sites = new Collection<Site>();
        }

        public Supplier(Guid id,
            string name,
            uint number,
            Guid correlationId,
            string correlationProvider,
            string? mailingAddress = default,
            string? city = default,
            string? province = default,
            string? postalCode = default)
           : base(id)
        {
            Name = name;
            Number = number;
            CorrelationId = correlationId;
            CorrelationProvider = correlationProvider;
            Sites = new Collection<Site>();
            MailingAddress = mailingAddress;
            City = city;
            Province = province;
            PostalCode = postalCode;
        }
    }
}
