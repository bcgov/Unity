using Microsoft.AspNetCore.Authorization;
using System;
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
            var applicantInfo =  await _applicationRepository.WithBasicDetailsAsync(applicationId);
            if (applicantInfo == null) return new ApplicationApplicantInfoDto();

            ApplicationApplicantInfoDto applicationApplicantInfoDto = new ApplicationApplicantInfoDto()
            {
                ApplicantId = applicationId,
                ApplicantName = applicantInfo.Applicant.ApplicantName,
                ApplicationReferenceNo = applicantInfo.ReferenceNo,
                ApplicationStatus = applicantInfo.ApplicationStatus.InternalStatus,
                ApplicationStatusCode = applicantInfo.ApplicationStatus.StatusCode,

                OrganizationName = applicantInfo.Applicant.OrgName,
                OrganizationSize = applicantInfo.Applicant.OrganizationSize,
                OrganizationType = applicantInfo.Applicant.OrganizationType,
                OrgNumber = applicantInfo.Applicant.OrgNumber,
                OrgStatus = applicantInfo.Applicant.OrgStatus,
                
                Sector = applicantInfo.Applicant.Sector,
                SectorSubSectorIndustryDesc = applicantInfo.Applicant.SectorSubSectorIndustryDesc,
                SubSector = applicantInfo.Applicant.SubSector,                

                SigningAuthorityBusinessPhone = applicantInfo.SigningAuthorityBusinessPhone ?? string.Empty,
                SigningAuthorityCellPhone = applicantInfo.SigningAuthorityCellPhone ?? string.Empty,
                SigningAuthorityEmail = applicantInfo.SigningAuthorityEmail ?? string.Empty,
                SigningAuthorityFullName = applicantInfo.SigningAuthorityFullName ?? string.Empty,
                SigningAuthorityTitle = applicantInfo.SigningAuthorityTitle ?? string.Empty,

                ContactFullName = applicantInfo.ApplicantAgent?.Name ?? string.Empty,
                ContactTitle = applicantInfo.ApplicantAgent?.Title ?? string.Empty,
                ContactEmail = applicantInfo.ApplicantAgent?.Email ?? string.Empty,
                ContactBusinessPhone = applicantInfo.ApplicantAgent?.Phone ?? string.Empty,
                ContactCellPhone = applicantInfo.ApplicantAgent?.Phone2 ?? string.Empty
            };

            foreach (ApplicantAddress item in applicantInfo.Applicant?.ApplicantAddresses ?? [])
            {
                applicationApplicantInfoDto.ApplicantAddresses.Add(ObjectMapper.Map<ApplicantAddress, ApplicantAddressDto>(item));
            }

            return applicationApplicantInfoDto;
        }
    }
}
