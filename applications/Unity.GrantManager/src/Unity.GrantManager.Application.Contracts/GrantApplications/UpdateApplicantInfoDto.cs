using System;
using System.Collections.Generic;
using Unity.Flex.Worksheets;
using Volo.Abp.Application.Dtos;

namespace Unity.GrantManager.GrantApplications;
public class UpdateApplicantInfoDto : CustomDataFieldDto
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
    public AddressType AddressType { get; set; }
    public string? Street { get; set; }
    public string? Street2 { get; set; }
    public string? Unit { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public string? PostalCode { get; set; }
}

public class ProjectLocationDto
{
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
}

public class SigningAuthorityDto
{
    public string? SigningAuthorityFullName { get; set; }
    public string? SigningAuthorityTitle { get; set; }
    public string? SigningAuthorityEmail { get; set; }
    public string? SigningAuthorityBusinessPhone { get; set; }
    public string? SigningAuthorityCellPhone { get; set; }
}

public class ContactInfoDto
{
    public string? ContactFullName { get; set; }
    public string? ContactTitle { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactBusinessPhone { get; set; }
    public string? ContactCellPhone { get; set; }
}
