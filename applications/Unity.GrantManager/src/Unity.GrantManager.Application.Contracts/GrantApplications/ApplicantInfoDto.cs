using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Unity.Flex.Worksheets;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Auditing;

namespace Unity.GrantManager.GrantApplications;
public class ApplicantInfoDto : CustomDataFieldDto
{
    public Guid ApplicationId { get; set; }
    public Guid ApplicantId { get; set; }
    public Guid ApplicationFormId { get; set; }
    
    public string ApplicationReferenceNo { get; set; } = string.Empty;
    public string ApplicantName { get; set; } = string.Empty;
    public GrantApplicationState ApplicationStatusCode { get; set; }

    // Nullable for zones
    public ApplicantSummaryDto? ApplicantSummary { get; set; }
    public ApplicantSupplierDto? ApplicantSupplier { get; set; }
    public List<ApplicantAddressDto>? ApplicantAddresses { get; set; }
    public SigningAuthorityDto? SigningAuthority { get; set; }
    public ContactInfoDto? ContactInfo { get; set; }
}

public class ApplicantSupplierDto
{
    public Guid SiteId { get; set; } = Guid.Empty;
    public Guid SupplierId { get; set; } = Guid.Empty;
    public string? SupplierNumber { get; set; }
    public string? OriginalSupplierNumber { get; set; }
}

public class ApplicantSummaryDto
{
    public string? OrgName { get; set; }
    public string? OrgNumber { get; set; }
    public string? OrgStatus { get; set; } // TODO
    public string? OrganizationType { get; set; } // TODO

    public string? NonRegOrgName { get; set; }
    public string? OrganizationSize { get; set; }
    public string? IndigenousOrgInd { get; set; }

    public string? UnityApplicantId { get; set; }
    public string? FiscalDay { get; set; }
    public string? FiscalMonth { get; set; }

    public string? Sector { get; set; } // TODO
    public string? SubSector { get; set; } // TODO
    public bool RedStop { get; set; } = false;
    public string? SectorSubSectorIndustryDesc { get; set; }
}

public class SigningAuthorityDto
{
    public Guid? ApplicationId { get; set; }
    public string? SigningAuthorityFullName { get; set; }
    public string? SigningAuthorityTitle { get; set; }

    //[EmailAddress]
    public string? SigningAuthorityEmail { get; set; }
    
    //[Phone]
    public string? SigningAuthorityBusinessPhone { get; set; }

    //[Phone]
    public string? SigningAuthorityCellPhone { get; set; }
}

public class ContactInfoDto
{
    public Guid? ApplicantAgentId { get; set; }
    public Guid? ApplicationId { get; set; }

    public string? Name { get; set; }
    public string? Title { get; set; }

    // [EmailAddress]
    public string? Email { get; set; }
    
    // [Phone]
    public string? Phone { get; set; }
    
    // [Phone]
    public string? Phone2 { get; set; }
}
