using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Integrations.Chefs;
using Volo.Abp.Domain.Entities;

namespace Unity.GrantManager.Intakes
{
    [Authorize]
    public class ApplicationIntakeAdminService(CustomFieldsIntakeSubmissionMapper customFieldsIntakeSubmissionMapper,
        IApplicationRepository applicationRepository,
        IApplicationFormSubmissionRepository applicationFormSubmissionsRepository,
        IApplicationFormVersionRepository applicationFormVersionRepository,
        IApplicationFormRepository applicationFormRepository,
        ISubmissionsApiService submissionsApiService)
        : GrantManagerAppService
    {
        /// <summary>
        /// Fix any missing worksheet instances that may have failed to create during intake
        /// </summary>
        /// <param name="referenceNo"></param>
        /// <returns></returns>
        /// <exception cref="EntityNotFoundException"></exception>
        public async Task FixMissingWorksheetsAsync(string referenceNo, Guid tenantId)
        {
            using (CurrentTenant.Change(tenantId))
            {
                var application =
                (await applicationRepository.GetQueryableAsync())
                .FirstOrDefault(a => a.ReferenceNo == referenceNo)
                    ?? throw new EntityNotFoundException("Application not found");

                var submission =
                    (await applicationFormSubmissionsRepository.GetQueryableAsync())
                    .FirstOrDefault(s => s.ApplicationId == application.Id)
                        ?? throw new EntityNotFoundException("Application Form Submission not found");

                var applicationForm = await applicationFormRepository.GetAsync(application.ApplicationFormId)
                        ?? throw new EntityNotFoundException("Application Form not found");

                if (applicationForm.ChefsApplicationFormGuid == null)
                {
                    throw new EntityNotFoundException("Chefs Application Form Guid not found for the form version");
                }

                var formVersionId = submission.ApplicationFormVersionId
                    ?? throw new EntityNotFoundException("Application Form Version not found for the submission");

                var formVersion = await applicationFormVersionRepository.GetAsync(formVersionId);

                JObject? submissionData = await submissionsApiService
                    .GetSubmissionDataAsync(Guid.Parse(applicationForm.ChefsApplicationFormGuid), Guid.Parse(submission.ChefsSubmissionGuid))
                        ?? throw new EntityNotFoundException("No Submission retrieved from CHEFS");

                await customFieldsIntakeSubmissionMapper.MapAndPersistCustomFields(
                    application.Id,
                    formVersionId,
                    submissionData,
                    formVersion.SubmissionHeaderMapping);
            }
        }
    }
}
