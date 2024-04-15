using System.Collections.ObjectModel;
using System;
using Volo.Abp.Application.Dtos;

namespace Unity.Payments.Suppliers
{
#pragma warning disable CS8618
    public class SupplierDto : ExtensibleFullAuditedEntityDto<Guid>
    {        
        public string? Name { get; set; } = string.Empty;
        public string? Number { get; set; } = string.Empty;

        /* Address */
        public string? MailingAddress { get; private set; }
        public string? City { get; private set; }
        public string? Province { get; private set; }
        public string? PostalCode { get; private set; }

        public Collection<SiteDto> Sites { get; private set; }
    }
#pragma warning restore CS8618
}
