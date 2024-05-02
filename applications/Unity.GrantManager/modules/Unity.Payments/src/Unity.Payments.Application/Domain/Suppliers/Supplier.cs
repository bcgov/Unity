using System;
using System.Collections.ObjectModel;
using System.Linq;
using Unity.Payments.Enums;
using Unity.Modules.Shared.Correlation;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.Payments.Domain.Suppliers
{
    public class Supplier : FullAuditedAggregateRoot<Guid>, IMultiTenant, ICorrelationEntity
    {
        public Guid? TenantId { get; set; }
        public virtual string? Name { get; set; } = string.Empty;
        public virtual string? Number { get; set; } = string.Empty;
        public virtual Collection<Site> Sites { get; private set; }

        /* Address */
        public virtual string? MailingAddress { get; private set; }
        public virtual string? City { get; private set; }
        public virtual string? Province { get; private set; }
        public virtual string? PostalCode { get; private set; }

        // External Correlation
        public virtual string CorrelationProvider { get; set; } = string.Empty;
        public virtual Guid CorrelationId { get; set; }

        protected Supplier()
        {
            /* This constructor is for ORMs to be used while getting the entity from the database. */
            Sites = new Collection<Site>();
        }

        public Supplier(Guid id,
            string? name,
            string? number,
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

        public Supplier AddSite(Site site)
        {
            /* Business rules around adding a site to a supplier */

            Sites.Add(site);

            return this;
        }

        public Supplier UpdateSite(Guid id,
            string number,
            PaymentGroup paymentGroup,
            string? addressLine1,
            string? addressLine2,
            string? addressLine3,
            string? city,
            string? province,
            string? postalCode)
        {

            /* Business rules around updating a site */

            var site = Sites.FirstOrDefault(s => s.Id == id);
            if (site == null) return this;

            site.SetNumber(number);
            site.SetPaymentGroup(paymentGroup);
            site.SetAddress(addressLine1, addressLine2, addressLine3, city, province, postalCode);

            return this;
        }

        public void SetName(string? name)
        {
            Name = name;
        }

        public void SetNumber(string? number)
        {
            Number = number;
        }

        public void SetAddress(string? mailingAddress,
            string? city,
            string? province,
            string? postalCode)
        {
            /* Business rules aournd update mailing address */

            MailingAddress = mailingAddress;
            City = city;
            Province = province;
            PostalCode = postalCode;
        }
    }
}
