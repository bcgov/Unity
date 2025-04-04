using System;
using Unity.Payments.Domain.Suppliers.ValueObjects;
using Unity.Payments.Enums;
using Unity.Payments.Suppliers;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.Payments.Domain.Suppliers
{
    public class Site : FullAuditedEntity<Guid>, IMultiTenant
    {
        public Guid? TenantId { get; set; }
        public virtual string Number { get; set; } = string.Empty;
        public virtual PaymentGroup PaymentGroup { get; set; }

        /* Address */
        public virtual string? AddressLine1 { get; set; }
        public virtual string? AddressLine2 { get; set; }
        public virtual string? AddressLine3 { get; set; }
        public virtual string? City { get; set; }
        public virtual string? Province { get; set; }
        public virtual string? PostalCode { get; set; }
        public virtual string? Country { get; set; }

        /* CAS Information */
        public virtual string? EmailAddress { get; set; }
        public virtual string? EFTAdvicePref { get; set; }
        public virtual string? BankAccount { get; set; }
        public virtual string? ProviderId { get; set; }
        public virtual string? Status { get; set; }
        public virtual string? SiteProtected { get; set; }
        public DateTime? LastUpdatedInCas { get; set; }

        /* Supplier */
        public virtual Supplier? Supplier { get; set; }
        public virtual Guid SupplierId { get; set; }
        public virtual bool MarkDeletedInUse { get; set; }

        protected Site()
        {
            /* This constructor is for ORMs to be used while getting the entity from the database. */
        }

        public Site(Guid id,
        string number,
        PaymentGroup paymentMethod,
        Address? address = default)
       : base(id)
        {
            Number = number;
            PaymentGroup = paymentMethod;
            AddressLine1 = address?.AddressLine1;
            AddressLine2 = address?.AddressLine2;
            AddressLine3 = address?.AddressLine3;
            Country = address?.Country;
            City = address?.City;
            Province = address?.Province;
            PostalCode = address?.PostalCode;
        }

        public Site(SiteDto siteDto)
        {
            Number = siteDto.Number;
            SupplierId = siteDto.SupplierId;
            EmailAddress = siteDto.EmailAddress;
            EFTAdvicePref = siteDto.EFTAdvicePref;
            BankAccount = siteDto.BankAccount;
            ProviderId = siteDto.ProviderId;
            Status = siteDto.Status;
            SiteProtected = siteDto.SiteProtected;
            PaymentGroup = siteDto.PaymentGroup;
            AddressLine1 = siteDto.AddressLine1;
            AddressLine2 = siteDto?.AddressLine2;
            AddressLine3 = siteDto?.AddressLine3;
            Country = siteDto?.Country;
            City = siteDto?.City;
            Province = siteDto?.Province;
            PostalCode = siteDto?.PostalCode;
            LastUpdatedInCas = siteDto?.LastUpdatedInCas;
            MarkDeletedInUse = siteDto?.MarkDeletedInUse ?? false;
        }

        public static bool SiteMatchesSiteDto(Site site, SiteDto siteDto)
        {
            return site.Number == siteDto.Number &&
                   site.SupplierId == siteDto.SupplierId &&
                   site.EmailAddress == siteDto.EmailAddress &&
                   site.EFTAdvicePref == siteDto.EFTAdvicePref &&
                   site.BankAccount == siteDto.BankAccount &&
                   site.ProviderId == siteDto.ProviderId &&
                   site.Status == siteDto.Status &&
                   site.SiteProtected == siteDto.SiteProtected &&
                   site.PaymentGroup == siteDto.PaymentGroup &&
                   site.AddressLine1 == siteDto.AddressLine1 &&
                   site.AddressLine2 == siteDto.AddressLine2 &&
                   site.AddressLine3 == siteDto.AddressLine3 &&
                   site.Country == siteDto.Country &&
                   site.City == siteDto.City &&
                   site.Province == siteDto.Province &&
                   site.PostalCode == siteDto.PostalCode &&
                   site.LastUpdatedInCas == siteDto.LastUpdatedInCas &&
                   site.MarkDeletedInUse == siteDto.MarkDeletedInUse;
        }

        public static Site UpdateSiteBySiteDto(Site site, SiteDto siteDto)
        {
            site.Number = siteDto.Number;
            site.SupplierId = siteDto.SupplierId;
            site.EmailAddress = siteDto.EmailAddress;
            site.EFTAdvicePref = siteDto.EFTAdvicePref;
            site.BankAccount = siteDto.BankAccount;
            site.ProviderId = siteDto.ProviderId;
            site.Status = siteDto.Status;
            site.SiteProtected = siteDto.SiteProtected;
            site.PaymentGroup = siteDto.PaymentGroup;
            site.AddressLine1 = siteDto.AddressLine1;
            site.AddressLine2 = siteDto?.AddressLine2;
            site.AddressLine3 = siteDto?.AddressLine3;
            site.Country = siteDto?.Country;
            site.City = siteDto?.City;
            site.Province = siteDto?.Province;
            site.PostalCode = siteDto?.PostalCode;
            site.LastUpdatedInCas = siteDto?.LastUpdatedInCas;
            site.MarkDeletedInUse = siteDto?.MarkDeletedInUse ?? false;
            return site;
        }

        public void SetNumber(string number)
        {
            Number = number;
        }

        public void SetPaymentGroup(PaymentGroup paymentGroup)
        {
            PaymentGroup = paymentGroup;
        }

        public void SetAddress(string? addressLine1,
            string? addressLine2,
            string? addressLine3,
            string? city,
            string? province,
            string? postalCode)
        {
            AddressLine1 = addressLine1;
            AddressLine2 = addressLine2;
            AddressLine3 = addressLine3;
            City = city;
            Province = province;
            PostalCode = postalCode;
        }
    }
}
