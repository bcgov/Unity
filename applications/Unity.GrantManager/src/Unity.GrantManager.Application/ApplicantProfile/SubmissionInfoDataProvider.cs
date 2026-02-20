using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Unity.GrantManager.ApplicantProfile.ProfileData;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Integrations;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;

namespace Unity.GrantManager.ApplicantProfile
{
    /// <summary>
    /// Provides submission information for the applicant profile by querying
    /// application form submissions linked to the applicant's OIDC subject.
    /// Resolves actual submission timestamps from CHEFS JSON data and derives
    /// the form view URL from the INTAKE_API_BASE dynamic URL setting.
    /// </summary>
    [ExposeServices(typeof(IApplicantProfileDataProvider))]
    public class SubmissionInfoDataProvider(
        ICurrentTenant currentTenant,
        IRepository<ApplicationFormSubmission, Guid> applicationFormSubmissionRepository,
        IRepository<Application, Guid> applicationRepository,
        IRepository<ApplicationStatus, Guid> applicationStatusRepository,
        IEndpointManagementAppService endpointManagementAppService,
        ILogger<SubmissionInfoDataProvider> logger)
        : IApplicantProfileDataProvider, ITransientDependency
    {
        /// <inheritdoc />
        public string Key => ApplicantProfileKeys.SubmissionInfo;

        /// <inheritdoc />
        public async Task<ApplicantProfileDataDto> GetDataAsync(ApplicantProfileInfoRequest request)
        {
            var dto = new ApplicantSubmissionInfoDto
            {
                Submissions = []
            };

            var subject = request.Subject ?? string.Empty;
            var normalizedSubject = subject.Contains('@')
                    ? subject[..subject.IndexOf('@')].ToUpperInvariant()
                    : subject.ToUpperInvariant();

            dto.LinkSource = await ResolveFormViewUrlAsync();

            using (currentTenant.Change(request.TenantId))
            {
                var submissionsQuery = await applicationFormSubmissionRepository.GetQueryableAsync();
                var applicationsQuery = await applicationRepository.GetQueryableAsync();
                var statusesQuery = await applicationStatusRepository.GetQueryableAsync();

                var results = await (
                    from submission in submissionsQuery
                    join application in applicationsQuery on submission.ApplicationId equals application.Id
                    join status in statusesQuery on application.ApplicationStatusId equals status.Id
                    where submission.OidcSub == normalizedSubject
                    select new
                    {
                        submission.Id,
                        LinkId = submission.ChefsSubmissionGuid,
                        submission.CreationTime,
                        submission.Submission,
                        application.ReferenceNo,
                        application.ProjectName,
                        Status = status.ExternalStatus
                    }).ToListAsync();

                dto.Submissions.AddRange(results.Select(s => new SubmissionInfoItemDto
                {
                    Id = s.Id,
                    LinkId = s.LinkId,
                    ReceivedTime = s.CreationTime,
                    SubmissionTime = ResolveSubmissionTime(s.Submission, s.CreationTime),
                    ReferenceNo = s.ReferenceNo,
                    ProjectName = s.ProjectName,
                    Status = s.Status
                }));
            }

            return dto;
        }

        /// <summary>
        /// Derives the CHEFS form view URL from the INTAKE_API_BASE dynamic URL setting.
        /// e.g. https://chefs-dev.apps.silver.devops.gov.bc.ca/app/api/v1
        ///   -> https://chefs-dev.apps.silver.devops.gov.bc.ca/app/form/view?s=
        /// </summary>
        private async Task<string> ResolveFormViewUrlAsync()
        {
            try
            {
                var chefsApiBaseUrl = await endpointManagementAppService.GetChefsApiBaseUrlAsync();
                var trimmed = chefsApiBaseUrl.TrimEnd('/');
                const string apiSegment = "/api/v1";
                if (trimmed.EndsWith(apiSegment, StringComparison.OrdinalIgnoreCase))
                {
                    trimmed = trimmed[..^apiSegment.Length];
                }
                return $"{trimmed}/form/view?s=";
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to resolve CHEFS form view URL from INTAKE_API_BASE setting.");
                return string.Empty;
            }
        }

        private DateTime ResolveSubmissionTime(string submissionJson, DateTime fallback)
        {
            try
            {
                if (!string.IsNullOrEmpty(submissionJson))
                {
                    using var doc = JsonDocument.Parse(submissionJson);
                    if (doc.RootElement.TryGetProperty("createdAt", out var createdAt) &&
                        createdAt.TryGetDateTime(out var dateTime))
                    {
                        return dateTime;
                    }
                }
            }
            catch (JsonException ex)
            {
                logger.LogWarning(ex, "Failed to parse submission JSON for submission time. Falling back to received time.");
            }

            return fallback;
        }
    }
}
