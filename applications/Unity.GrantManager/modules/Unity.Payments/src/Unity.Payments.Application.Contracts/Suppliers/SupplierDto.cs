using System.Collections.ObjectModel;
using System;
using Volo.Abp.Application.Dtos;

namespace Unity.Payments.Suppliers
{
#pragma warning disable CS8618
    public class SupplierDto : ExtensibleFullAuditedEntityDto<Guid>
    {        
        public string? Number { get; set; }
        public string? Name { get; set; }
        public string? Subcategory { get; set; }
        public string? SIN { get; set; }
        public string? ProviderId { get; set; }
        public string? BusinessNumber { get; set; }
        public string? Status { get; set; }
        public string? SupplierProtected { get; set; }
        public string? StandardIndustryClassification { get; set; }
        public DateTime? LastUpdatedInCAS { get; set; }

        /* Address */
        public string? MailingAddress { get; private set; }
        public string? City { get; private set; }
        public string? Province { get; private set; }
        public string? PostalCode { get; private set; }

        public Collection<SiteDto> Sites { get; private set; }
    }
#pragma warning restore CS8618
}
