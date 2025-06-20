using System;
using System.Collections.Generic;
using Unity.Flex.Worksheets;

namespace Unity.GrantManager.GrantApplications;

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
    public List<ApplicantAddressDto> ApplicantAddresses { get; set; } = [];
    public Guid ApplicationFormId { get; set; }
    public string NonRegOrgName { get; set; } = string.Empty;        
}

public class ApplicantInfoDto : CustomDataFieldDto
{
    public Guid ApplicationId { get; set; }
    public Guid ApplicantId { get; set; }
    public Guid ApplicationFormId { get; set; }

    public string ApplicationReferenceNo { get; set; } = string.Empty;
    public string ApplicantName { get; set; } = string.Empty;
    public GrantApplicationState ApplicationStatusCode { get; set; }

    public ApplicantSummaryDto? ApplicantSummary { get; set; }
    public List<ApplicantAddressDto>? ApplicantAddresses { get; set; }
    public SigningAuthorityDto? SigningAuthority { get; set; }
    public ContactInfoDto? ContactInfo { get; set; }
}

public class ApplicantSummaryDto : GrantApplicationApplicantDto
{
    public Guid ApplicantId { get; set; }
    public Guid ApplicationFormId { get; set; }
    public string NonRegOrgName { get; set; } = string.Empty;
}

public class SigningAuthorityDto
{
    public Guid? ApplicationId { get; set; }

    public string? SigningAuthorityFullName { get; set; }
    public string? SigningAuthorityTitle { get; set; }
    public string? SigningAuthorityEmail { get; set; }
    public string? SigningAuthorityBusinessPhone { get; set; }
    public string? SigningAuthorityCellPhone { get; set; }
}

public class ContactInfoDto
{
    public Guid? ApplicantAgentId { get; set; }
    public Guid? ApplicationId { get; set; }

    public string? Name { get; set; }
    public string? Title { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Phone2 { get; set; }
}
