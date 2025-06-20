using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.Modules.Shared;

namespace Unity.GrantManager.GrantApplications
{
    [Authorize]
    public class ApplicationApplicantAppService : GrantManagerAppService, IApplicationApplicantAppService
    {
        private readonly IApplicationRepository _applicationRepository;

        public ApplicationApplicantAppService(IApplicationRepository applicationRepository)
        {
            _applicationRepository = applicationRepository;
        }

        public async Task<ApplicantInfoDto> GetApplicantInfoTabAsync(Guid applicationId)
        {
            var application = await _applicationRepository.WithBasicDetailsAsync(applicationId);
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
            }
            else
            {
                applicantInfoDto.SigningAuthority = new SigningAuthorityDto();
            }

            //-- APPLICANT INFO ADDRESS
            if (await AuthorizationService.IsGrantedAsync(UnitySelector.Applicant.Location.Default))
            {
                //applicantInfoDto.ApplicantAddresses = ObjectMapper.Map<Application, List<ApplicantAddressDto>>(application);
                applicantInfoDto.ApplicantAddresses = ObjectMapper.Map<List<ApplicantAddress>, List<ApplicantAddressDto>>(application?.Applicant?.ApplicantAddresses?.ToList() ?? []);
            }
            else
            {
                applicantInfoDto.ApplicantAddresses = [];
            }

            return applicantInfoDto;
        }

        public async Task<ApplicationApplicantInfoDto> GetByApplicationIdAsync(Guid applicationId)
        {
            var applicantInfo = await _applicationRepository.WithBasicDetailsAsync(applicationId);
            if (applicantInfo == null) return new ApplicationApplicantInfoDto();

            return new ApplicationApplicantInfoDto()
            {
                ApplicantId = applicantInfo.Applicant.Id,
                ApplicationFormId = applicantInfo.ApplicationFormId,
                ApplicantName = applicantInfo.Applicant?.ApplicantName ?? string.Empty,
                ApplicationReferenceNo = applicantInfo.ReferenceNo,
                ApplicationStatus = applicantInfo.ApplicationStatus.InternalStatus,
                ApplicationStatusCode = applicantInfo.ApplicationStatus.StatusCode,

                OrganizationName = applicantInfo.Applicant?.OrgName ?? string.Empty,
                OrganizationSize = applicantInfo.Applicant?.OrganizationSize ?? string.Empty,
                OrganizationType = applicantInfo.Applicant?.OrganizationType ?? string.Empty,
                OrgNumber = applicantInfo.Applicant?.OrgNumber ?? string.Empty,
                OrgStatus = applicantInfo.Applicant?.OrgStatus ?? string.Empty,
                NonRegOrgName = applicantInfo.Applicant?.NonRegOrgName ?? string.Empty,

                Sector = applicantInfo.Applicant?.Sector ?? string.Empty,
                SectorSubSectorIndustryDesc = applicantInfo.Applicant?.SectorSubSectorIndustryDesc ?? string.Empty,
                SubSector = applicantInfo.Applicant?.SubSector ?? string.Empty,
                RedStop = applicantInfo.Applicant?.RedStop ?? false,
                IndigenousOrgInd = applicantInfo.Applicant?.IndigenousOrgInd ?? string.Empty,
                UnityApplicantId = applicantInfo.Applicant?.UnityApplicantId ?? string.Empty,
                FiscalDay = applicantInfo.Applicant?.FiscalDay.ToString() ?? string.Empty,
                FiscalMonth = applicantInfo.Applicant?.FiscalMonth ?? string.Empty,

                SigningAuthorityBusinessPhone = applicantInfo.SigningAuthorityBusinessPhone ?? string.Empty,
                SigningAuthorityCellPhone = applicantInfo.SigningAuthorityCellPhone ?? string.Empty,
                SigningAuthorityEmail = applicantInfo.SigningAuthorityEmail ?? string.Empty,
                SigningAuthorityFullName = applicantInfo.SigningAuthorityFullName ?? string.Empty,
                SigningAuthorityTitle = applicantInfo.SigningAuthorityTitle ?? string.Empty,

                ContactFullName = applicantInfo.ApplicantAgent?.Name ?? string.Empty,
                ContactTitle = applicantInfo.ApplicantAgent?.Title ?? string.Empty,
                ContactEmail = applicantInfo.ApplicantAgent?.Email ?? string.Empty,
                ContactBusinessPhone = applicantInfo.ApplicantAgent?.Phone ?? string.Empty,
                ContactCellPhone = applicantInfo.ApplicantAgent?.Phone2 ?? string.Empty,

                ApplicantAddresses = ObjectMapper.Map<List<ApplicantAddress>, List<ApplicantAddressDto>>(applicantInfo.Applicant?.ApplicantAddresses?.ToList() ?? []),
                ElectoralDistrict = applicantInfo.Applicant?.ElectoralDistrict ?? string.Empty
            };
        }
    }
}
