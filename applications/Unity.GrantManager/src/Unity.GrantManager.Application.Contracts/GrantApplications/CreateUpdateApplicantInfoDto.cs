using System;
using Unity.Flex.Worksheets;

namespace Unity.GrantManager.GrantApplications
{
    public class CreateUpdateApplicantInfoDto : CustomDataFieldDto
    {
        public string? OrgName { get; set; }
        public string? OrgNumber { get; set; }
        public string? OrgStatus { get; set; }
        public string? OrganizationType  { get; set; }
        public string? OrganizationSize { get; set; }

        public string? Sector { get; set; }
        public string? SubSector { get; set; }        
        public string? SectorSubSectorIndustryDesc { get; set; } = string.Empty;

        public string? ContactFullName { get; set; }
        public string? ContactTitle { get; set; }
        public string? ContactEmail { get; set; }
        public string? ContactBusinessPhone { get; set; }
        public string? ContactCellPhone { get; set; }
        public string? SigningAuthorityFullName { get; set; }
        public string? SigningAuthorityTitle { get; set; }
        public string? SigningAuthorityEmail { get; set; }
        public string? SigningAuthorityBusinessPhone { get; set; }
        public string? SigningAuthorityCellPhone { get; set; }
        public string? SupplierNumber { get; set; }
        public string? OriginalSupplierNumber { get; set; }
        public string? PhysicalAddressStreet { get; set; }
        public string? PhysicalAddressStreet2 { get; set; }
        public string? PhysicalAddressUnit { get; set; }
        public string? PhysicalAddressCity { get; set; }
        public string? PhysicalAddressProvince { get; set; }
        public string? PhysicalAddressPostalCode { get; set; }

        public string? MailingAddressStreet { get; set; }
        public string? MailingAddressStreet2 { get; set; }
        public string? MailingAddressUnit { get; set; }
        public string? MailingAddressCity { get; set; }
        public string? MailingAddressProvince { get; set; }
        public string? MailingAddressPostalCode { get; set; }

        public Guid ApplicantId {  get; set; }        
    }
}
