using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Unity.Flex.Worksheets;
using Volo.Abp.Application.Dtos;

namespace Unity.GrantManager.GrantApplications;
public class ApplicantInfoDto : CustomDataFieldDto
{
    public Guid ApplicantId { get; set; }

    public OrganizationInfoDto? OrganizationInfo { get; set; }
    public ApplicantSupplierDto? ApplicantSupplier { get; set; }
    public List<UpdateApplicantAddressDto>? ApplicantAddresses { get; set; }
    public SigningAuthorityDto? SigningAuthority { get; set; }
    public ContactInfoDto? ContactInfo { get; set; }
}

public class ApplicantSupplierDto
{
    public string? SupplierNumber { get; set; }
    public string? OriginalSupplierNumber { get; set; }
}

public class OrganizationInfoDto
{
    public Guid? ApplicantId { get; set; }
    public string? OrgName { get; set; }
    public string? OrgNumber { get; set; }
    public string? OrgStatus { get; set; }
    public string? OrganizationType { get; set; }
    public string? OrganizationSize { get; set; }
    public string? Sector { get; set; }
    public string? SubSector { get; set; }
    public string? SectorSubSectorIndustryDesc { get; set; }
    public bool? RedStop { get; set; }
    public string? IndigenousOrgInd { get; set; }
}

public class UpdateApplicantAddressDto : EntityDto<Guid>
{
    public Guid ApplicantId { get; set; }
    public AddressType AddressType { get; set; }
    public string? Street { get; set; }
    public string? Street2 { get; set; }
    public string? Unit { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public string? PostalCode { get; set; }
}

public class SigningAuthorityDto
{
    public Guid? ApplicationId { get; set; }
    public string? SigningAuthorityFullName { get; set; }
    public string? SigningAuthorityTitle { get; set; }

    [EmailAddress]
    public string? SigningAuthorityEmail { get; set; }
    
    [Phone]
    public string? SigningAuthorityBusinessPhone { get; set; }

    [Phone]
    public string? SigningAuthorityCellPhone { get; set; }
}

public class ContactInfoDto
{
    public Guid? ApplicantAgentId { get; set; }
    public string? ContactFullName { get; set; }
    public string? ContactTitle { get; set; }

    [EmailAddress]
    public string? ContactEmail { get; set; }
    
    [Phone]
    public string? ContactBusinessPhone { get; set; }
    
    [Phone]
    public string? ContactCellPhone { get; set; }
}
