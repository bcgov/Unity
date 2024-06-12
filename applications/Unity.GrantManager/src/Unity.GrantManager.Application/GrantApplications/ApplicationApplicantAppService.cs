using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;

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

                Sector = applicantInfo.Applicant?.Sector ?? string.Empty,
                SectorSubSectorIndustryDesc = applicantInfo.Applicant?.SectorSubSectorIndustryDesc ?? string.Empty,
                SubSector = applicantInfo.Applicant?.SubSector ?? string.Empty,

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

                ApplicantAddresses = ObjectMapper.Map<List<ApplicantAddress>, List<ApplicantAddressDto>>(applicantInfo.Applicant?.ApplicantAddresses?.ToList() ?? [])
            };
        }
    }
}
