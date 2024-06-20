using System;
using System.Collections.ObjectModel;
using System.Linq;
using Unity.Payments.Enums;
using Unity.Modules.Shared.Correlation;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;
using Unity.Payments.Domain.Suppliers.ValueObjects;

namespace Unity.Payments.Domain.Suppliers
{
    public class Supplier : FullAuditedAggregateRoot<Guid>, IMultiTenant, ICorrelationEntity
    {
        public Guid? TenantId { get; set; }
        public virtual string? Name { get; set; } = string.Empty;
        public virtual string? Number { get; set; } = string.Empty;
        public virtual string? Subcategory { get; set; } = string.Empty;
        public virtual string? SIN { get; set; } = string.Empty;
        public virtual string? ProviderId { get; set; } = string.Empty;
        public virtual string? BusinessNumber { get; set; } = string.Empty;
        public virtual string? Status { get; set; } = string.Empty;
        public virtual string? SupplierProtected { get; set; } = string.Empty;
        public virtual string? StandardIndustryClassification { get; set; } = string.Empty;
        public virtual DateTime? LastUpdatedInCAS { get; set; }
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
            MailingAddress? mailingAddress = default)
           : base(id)
        {
            Name = name;
            Number = number;
            CorrelationId = correlationId;
            CorrelationProvider = correlationProvider;
            Sites = new Collection<Site>();
            MailingAddress = mailingAddress?.AddressLine;
            City = mailingAddress?.City;
            Province = mailingAddress?.Province;
            PostalCode = mailingAddress?.PostalCode;
        }

        public Supplier(Guid id,
            string? name,
            string? number,
            string? subcategory,
            string? providerId,
            string? businessNumber,
            string? status,
            string? supplierProtected,
            string? standardIndustryClassification,
            DateTime? lastUpdatedInCAS,
            Guid correlationId,
            string correlationProvider,
            MailingAddress? mailingAddress = default)
           : base(id)
        {
            Name = name;
            Number = number;
            Subcategory = subcategory;
            ProviderId = providerId;
            BusinessNumber = businessNumber;
            Status = status;
            SupplierProtected = supplierProtected;
            StandardIndustryClassification = standardIndustryClassification;
            LastUpdatedInCAS = lastUpdatedInCAS;
            CorrelationId = correlationId;
            CorrelationProvider = correlationProvider;
            Sites = new Collection<Site>();
            MailingAddress = mailingAddress?.AddressLine;
            City = mailingAddress?.City;
            Province = mailingAddress?.Province;
            PostalCode = mailingAddress?.PostalCode;
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
