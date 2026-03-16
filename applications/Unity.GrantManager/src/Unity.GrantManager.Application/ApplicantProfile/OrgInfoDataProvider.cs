using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Unity.GrantManager.ApplicantProfile.ProfileData;
using Unity.GrantManager.Applications;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;

namespace Unity.GrantManager.ApplicantProfile
{
    /// <summary>
    /// Provides organization information for the applicant profile by querying
    /// applicants linked to the applicant's form submissions via OIDC subject.
    /// </summary>
    [ExposeServices(typeof(IApplicantProfileDataProvider))]
    public class OrgInfoDataProvider(
        ICurrentTenant currentTenant,
        IRepository<ApplicationFormSubmission, Guid> applicationFormSubmissionRepository,
        IRepository<Applicant, Guid> applicantRepository)
        : IApplicantProfileDataProvider, ITransientDependency
    {
        /// <inheritdoc />
        public string Key => ApplicantProfileKeys.OrgInfo;

        /// <inheritdoc />
        public async Task<ApplicantProfileDataDto> GetDataAsync(ApplicantProfileInfoRequest request)
        {
            var dto = new ApplicantOrgInfoDto
            {
                Organizations = []
            };

            var normalizedSubject = SubjectNormalizer.Normalize(request.Subject);
            if (normalizedSubject is null) return dto;

            using (currentTenant.Change(request.TenantId))
            {
                var submissionsQuery = await applicationFormSubmissionRepository.GetQueryableAsync();
                var applicantsQuery = await applicantRepository.GetQueryableAsync();

                var results = await (
                    from submission in submissionsQuery
                    join applicant in applicantsQuery on submission.ApplicantId equals applicant.Id
                    where submission.OidcSub == normalizedSubject
                    select new
                    {
                        applicant.Id,
                        applicant.OrgName,
                        applicant.OrganizationType,
                        applicant.OrgNumber,
                        applicant.OrgStatus,
                        applicant.NonRegOrgName,
                        applicant.FiscalMonth,
                        applicant.FiscalDay,
                        applicant.OrganizationSize,
                        applicant.Sector,
                        applicant.SubSector
                    })
                    .ToListAsync();

                dto.Organizations.AddRange(results.Select(r => new OrgInfoItemDto
                {
                    Id = r.Id,
                    OrgName = r.OrgName,
                    OrganizationType = r.OrganizationType,
                    OrgNumber = r.OrgNumber,
                    OrgStatus = r.OrgStatus,
                    NonRegOrgName = r.NonRegOrgName,
                    FiscalMonth = r.FiscalMonth,
                    FiscalDay = r.FiscalDay,
                    OrganizationSize = r.OrganizationSize,
                    Sector = r.Sector,
                    SubSector = r.SubSector
                }));
            }

            return dto;
        }
    }
}
