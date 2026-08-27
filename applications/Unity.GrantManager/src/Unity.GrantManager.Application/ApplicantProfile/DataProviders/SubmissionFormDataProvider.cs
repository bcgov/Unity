using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Unity.GrantManager.ApplicantProfile.ProfileData;
using Unity.GrantManager.Applications;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.DependencyInjection;
using Volo.Abp.MultiTenancy;

namespace Unity.GrantManager.ApplicantProfile
{
    /// <summary>
    /// Provides the form.io schema and submitted answers for a single submission, so the
    /// Applicant Portal can render a client-side PDF (see AB#34070). Unlike the other
    /// applicant profile data providers, an unknown or unowned <see cref="ApplicantProfileInfoRequest.SubmissionId"/>
    /// results in an <see cref="EntityNotFoundException"/> (mapped to 404 by the controller)
    /// rather than an empty payload, since the caller must not be able to distinguish
    /// "not found" from "not yours" for PII/financial data.
    /// </summary>
    [ExposeServices(typeof(IApplicantProfileDataProvider))]
    public class SubmissionFormDataProvider(
        ICurrentTenant currentTenant,
        IRepository<ApplicationFormSubmission, Guid> applicationFormSubmissionRepository,
        IApplicationFormVersionRepository applicationFormVersionRepository,
        ILogger<SubmissionFormDataProvider> logger)
        : IApplicantProfileDataProvider, ITransientDependency
    {
        /// <inheritdoc />
        public string Key => ApplicantProfileKeys.SubmissionFormData;

        /// <inheritdoc />
        public async Task<ApplicantProfileDataDto> GetDataAsync(ApplicantProfileInfoRequest request)
        {
            var normalizedSubject = SubjectNormalizer.Normalize(request.Subject);
            if (normalizedSubject is null || request.SubmissionId is null || request.SubmissionId == Guid.Empty)
            {
                throw new EntityNotFoundException("Submission not found.");
            }

            using (currentTenant.Change(request.TenantId))
            {
                var submissionsQuery = await applicationFormSubmissionRepository.GetQueryableAsync();
                var submission = await submissionsQuery
                    .Where(s => s.Id == request.SubmissionId.Value && s.OidcSub == normalizedSubject)
                    .FirstOrDefaultAsync();

                if (submission == null)
                {
                    logger.LogWarning("Submission {SubmissionId} was not found or is not owned by the requesting applicant.", request.SubmissionId);
                    throw new EntityNotFoundException("Submission not found.");
                }

                var formVersion = await ResolveFormVersionAsync(submission);
                if (string.IsNullOrWhiteSpace(formVersion?.FormSchema))
                {
                    logger.LogWarning("No form schema is available for submission {SubmissionId}.", request.SubmissionId);
                    throw new EntityNotFoundException("Submission form schema not available.");
                }

                var submissionData = ExtractSubmissionData(submission.Submission);
                if (submissionData is null)
                {
                    logger.LogWarning("No submission data is available for submission {SubmissionId}.", request.SubmissionId);
                    throw new EntityNotFoundException("Submission data not available.");
                }

                using var schemaDocument = JsonDocument.Parse(formVersion.FormSchema);

                return new ApplicantSubmissionFormDataDto
                {
                    Schema = schemaDocument.RootElement.Clone(),
                    Data = submissionData.Value
                };
            }
        }

        private async Task<ApplicationFormVersion?> ResolveFormVersionAsync(ApplicationFormSubmission submission)
        {
            if (submission.ApplicationFormVersionId is { } applicationFormVersionId)
            {
                var formVersion = await applicationFormVersionRepository.FindAsync(applicationFormVersionId);
                if (formVersion != null)
                {
                    return formVersion;
                }
            }

            return submission.FormVersionId is { } chefsFormVersionId
                ? await applicationFormVersionRepository.GetByChefsFormVersionAsync(chefsFormVersionId)
                : null;
        }

        /// <summary>
        /// Extracts the form.io submission object (<c>{ "data": {...}, "state": ..., "metadata": ... }</c>)
        /// nested under the <c>submission</c> property of the stored CHEFS submission resource JSON.
        /// </summary>
        private static JsonElement? ExtractSubmissionData(string submissionJson)
        {
            if (string.IsNullOrWhiteSpace(submissionJson))
            {
                return null;
            }

            try
            {
                using var doc = JsonDocument.Parse(submissionJson);
                return doc.RootElement.TryGetProperty("submission", out var submissionElement)
                    ? submissionElement.Clone()
                    : null;
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}
