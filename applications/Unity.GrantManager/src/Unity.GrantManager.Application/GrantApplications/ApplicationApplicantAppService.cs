using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Flex;
using Unity.Modules.Shared;
using Volo.Abp.Domain.Entities;

namespace Unity.GrantManager.GrantApplications;

[Authorize]
public class ApplicationApplicantAppService(
    IApplicationRepository applicationRepository, IApplicantRepository applicantRepository) : GrantManagerAppService, IApplicationApplicantAppService
{
    public async Task<ApplicationApplicantInfoDto> GetByApplicationIdAsync(Guid applicationId)
    {
        var applicantInfo = await applicationRepository.WithBasicDetailsAsync(applicationId);
        if (applicantInfo == null) return new ApplicationApplicantInfoDto();

        var appInfoDto = new ApplicationApplicantInfoDto()
        {
            ApplicantId                   = applicantInfo.Applicant.Id,
            ApplicationFormId             = applicantInfo.ApplicationFormId,
            ApplicantName                 = applicantInfo.Applicant?.ApplicantName ?? string.Empty,
            ApplicationReferenceNo        = applicantInfo.ReferenceNo,
            ApplicationStatus             = applicantInfo.ApplicationStatus.InternalStatus,
            ApplicationStatusCode         = applicantInfo.ApplicationStatus.StatusCode,
            OrganizationName              = applicantInfo.Applicant?.OrgName ?? string.Empty,
            OrganizationSize              = applicantInfo.Applicant?.OrganizationSize ?? string.Empty,
            OrganizationType              = applicantInfo.Applicant?.OrganizationType ?? string.Empty,
            OrgNumber                     = applicantInfo.Applicant?.OrgNumber ?? string.Empty,
            OrgStatus                     = applicantInfo.Applicant?.OrgStatus ?? string.Empty,
            NonRegOrgName                 = applicantInfo.Applicant?.NonRegOrgName ?? string.Empty,
            Sector                        = applicantInfo.Applicant?.Sector ?? string.Empty,
            SectorSubSectorIndustryDesc   = applicantInfo.Applicant?.SectorSubSectorIndustryDesc ?? string.Empty,
            SubSector                     = applicantInfo.Applicant?.SubSector ?? string.Empty,
            RedStop                       = applicantInfo.Applicant?.RedStop ?? false,
            IndigenousOrgInd              = applicantInfo.Applicant?.IndigenousOrgInd ?? string.Empty,
            UnityApplicantId              = applicantInfo.Applicant?.UnityApplicantId ?? string.Empty,
            FiscalDay                     = applicantInfo.Applicant?.FiscalDay.ToString() ?? string.Empty,
            FiscalMonth                   = applicantInfo.Applicant?.FiscalMonth ?? string.Empty,
            
            SigningAuthorityBusinessPhone = applicantInfo.SigningAuthorityBusinessPhone ?? string.Empty,
            SigningAuthorityCellPhone     = applicantInfo.SigningAuthorityCellPhone ?? string.Empty,
            SigningAuthorityEmail         = applicantInfo.SigningAuthorityEmail ?? string.Empty,
            SigningAuthorityFullName      = applicantInfo.SigningAuthorityFullName ?? string.Empty,
            SigningAuthorityTitle         = applicantInfo.SigningAuthorityTitle ?? string.Empty,
            
            ContactFullName               = applicantInfo.ApplicantAgent?.Name ?? string.Empty,
            ContactTitle                  = applicantInfo.ApplicantAgent?.Title ?? string.Empty,
            ContactEmail                  = applicantInfo.ApplicantAgent?.Email ?? string.Empty,
            ContactBusinessPhone          = applicantInfo.ApplicantAgent?.Phone ?? string.Empty,
            ContactCellPhone              = applicantInfo.ApplicantAgent?.Phone2 ?? string.Empty,
            ApplicantAddresses            = ObjectMapper.Map<List<ApplicantAddress>, List<ApplicantAddressDto>>(applicantInfo.Applicant?.ApplicantAddresses?.ToList() ?? []),
            ElectoralDistrict             = applicantInfo.Applicant?.ElectoralDistrict ?? string.Empty
        };
        return appInfoDto;
    }

    [Authorize(UnitySelector.Applicant.Summary.Default)]
    public async Task<ApplicantSummaryDto> GetApplicantSummaryAsync(Guid applicantId)
    {
        var applicant = await applicantRepository.GetAsync(applicantId);
        return ObjectMapper.Map<Applicant, ApplicantSummaryDto>(applicant);
    }

    [Authorize(UnitySelector.Applicant.Summary.Update)]
    public async Task<Applicant> UpdateApplicantSummaryAsync(Guid applicantId, ApplicantSummaryDto inputApplicantSummary)
    {
        var applicant = await applicantRepository.GetAsync(applicantId) ?? throw new EntityNotFoundException();

        // TODO: Validation
        // TODO: Status based value updates

        // Sanitize null inputs
        inputApplicantSummary.OrgName                     ??= applicant.OrgName;
        inputApplicantSummary.OrgNumber                   ??= applicant.OrgNumber;
        inputApplicantSummary.OrgStatus                   ??= applicant.OrgStatus;
        inputApplicantSummary.OrganizationType            ??= applicant.OrganizationType;
        inputApplicantSummary.NonRegOrgName               ??= applicant.NonRegOrgName;
        inputApplicantSummary.OrganizationSize            ??= applicant.OrganizationSize;
        inputApplicantSummary.IndigenousOrgInd            ??= applicant.IndigenousOrgInd;
        inputApplicantSummary.UnityApplicantId            ??= applicant.UnityApplicantId;
        inputApplicantSummary.FiscalDay                   ??= applicant.FiscalDay.ToString();
        inputApplicantSummary.FiscalMonth                 ??= applicant.FiscalMonth;
        inputApplicantSummary.Sector                      ??= applicant.Sector;
        inputApplicantSummary.SubSector                   ??= applicant.SubSector;
        inputApplicantSummary.SectorSubSectorIndustryDesc ??= applicant.SectorSubSectorIndustryDesc;

        ObjectMapper.Map<ApplicantSummaryDto, Applicant>(inputApplicantSummary, applicant);
        
        return await applicantRepository.UpdateAsync(applicant);
    }

    public async Task<ApplicantInfoDto> GetApplicantInfoTabAsync(Guid applicationId)
    {
        var application = await applicationRepository.WithBasicDetailsAsync(applicationId);
        if (application == null || !await AuthorizationService.IsGrantedAsync(UnitySelector.Applicant.Default))
        {
            return new ApplicantInfoDto();
        }
        
        var applicantInfoDto = ObjectMapper.Map<Application, ApplicantInfoDto>(application);

        //-- APPLICANT INFO SUMMARY
        if (await AuthorizationService.IsGrantedAsync(UnitySelector.Applicant.Summary.Default))
        {
            applicantInfoDto.ApplicantSummary = ObjectMapper.Map<Applicant, ApplicantSummaryDto>(application.Applicant);
            applicantInfoDto.ApplicantSummary.FiscalDay = application.Applicant?.FiscalDay.ToString() ?? string.Empty;
        }
        else
        {
            applicantInfoDto.ApplicantSummary = new ApplicantSummaryDto();
        }

        //-- APPLICANT INFO SUPPLIER

        //-- APPLICANT INFO CONTACT
        if (application?.ApplicantAgent is not null && await AuthorizationService.IsGrantedAsync(UnitySelector.Applicant.Contact.Default))
        {
            applicantInfoDto.ContactInfo = ObjectMapper.Map<ApplicantAgent, ContactInfoDto>(application.ApplicantAgent);
        }
        else
        {
            applicantInfoDto.ContactInfo = new ContactInfoDto();
        }

        //-- SIGNING AUTHORITY
        if (application != null && await AuthorizationService.IsGrantedAsync(UnitySelector.Applicant.Authority.Default))
        {
            applicantInfoDto.SigningAuthority = ObjectMapper.Map<Application, SigningAuthorityDto>(application);

            //applicantInfoDto.SigningAuthority.SigningAuthorityFullName      = application.SigningAuthorityFullName ?? string.Empty;
            //applicantInfoDto.SigningAuthority.SigningAuthorityTitle         = application.SigningAuthorityTitle ?? string.Empty;
            //applicantInfoDto.SigningAuthority.SigningAuthorityEmail         = application.SigningAuthorityEmail ?? string.Empty;
            //applicantInfoDto.SigningAuthority.SigningAuthorityBusinessPhone = application.SigningAuthorityBusinessPhone ?? string.Empty;
            //applicantInfoDto.SigningAuthority.SigningAuthorityCellPhone     = application.SigningAuthorityCellPhone ?? string.Empty;
        }
        else
        {
            applicantInfoDto.SigningAuthority = new SigningAuthorityDto();
        }

        //-- APPLICANT INFO ADDRESS
        if (await AuthorizationService.IsGrantedAsync(UnitySelector.Applicant.Location.Default))
        {
            //applicantInfoDto.ApplicantAddresses = ObjectMapper.Map<Application, List<ApplicantAddressDto>>(application);
        }
        else
        {
            applicantInfoDto.ApplicantAddresses = [];
        }


        // MAP SIGNING AUTHORITY
        //if (!await AuthorizationService.IsGrantedAsync(UnitySelector.Applicant.Summary.Default))
        //{
        //    applicantInfoDto.ApplicantSummary = ObjectMapper.Map<Application, ApplicantSummaryDto>(application);
        //}

        return applicantInfoDto;
    }
}
