using System;
using System.Collections.Generic;

namespace Unity.GrantManager.GrantApplications
{
    [Serializable]
    public class ApplicationApplicantInfoDto : GrantApplicationApplicantDto
    {
        public Guid ApplicantId { get; set; }
        public string ContactFullName { get; set; } = string.Empty;
        public string ContactTitle { get; set; } = string.Empty;
        public string ContactEmail { get; set; } = string.Empty;
        public string ContactBusinessPhone { get; set; } = string.Empty;
        public string ContactCellPhone { get; set; } = string.Empty;
        public string OrganizationName { get; set; } = string.Empty;
        public string SigningAuthorityFullName { get; set; } = string.Empty;
        public string SigningAuthorityTitle { get; set; } = string.Empty;
        public string SigningAuthorityEmail { get; set; } = string.Empty;
        public string SigningAuthorityBusinessPhone { get; set; } = string.Empty;
        public string SigningAuthorityCellPhone { get; set; } = string.Empty;
        public string ApplicationReferenceNo { get; set; } = string.Empty;
        public string ApplicationStatus { get; set; } = string.Empty;
        public GrantApplicationState ApplicationStatusCode { get; set; }
        public List<ApplicantAddressDto> ApplicantAddresses { get; set; } = new List<ApplicantAddressDto>();
        public Guid ApplicationFormId { get; set; }
    }
}
