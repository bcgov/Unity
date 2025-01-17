using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Entities;

namespace Unity.GrantManager.Reporting.DataGenerators
{
    [Authorize]
    public class SubmissionReportingDataGeneratorAppService(IReportingDataGenerator reportingDataGenerator,
        IApplicationFormSubmissionRepository applicationFormSubmissionRepository,
        IApplicationFormVersionRepository applicationFormVersionRepository) : ApplicationService, ISubmissionReportingDataGeneratorAppService
    {
        public async Task Generate(Guid submissionId)
        {
            var submission = await applicationFormSubmissionRepository.GetAsync(submissionId);
            Guid applicationFormVersionId;
            ApplicationFormVersion? applicationFormVersion;

            if (submission.ApplicationFormVersionId == null)
            {
                applicationFormVersion = await applicationFormVersionRepository.GetByChefsFormVersionAsync(submission.FormVersionId ?? Guid.Empty);

                if (applicationFormVersion == null)
                {
                    throw new EntityNotFoundException();
                }

                applicationFormVersionId = applicationFormVersion.Id;
            }
            else
            {
                applicationFormVersionId = submission.ApplicationFormVersionId.Value;
            }

            applicationFormVersion = await applicationFormVersionRepository.GetAsync(applicationFormVersionId) ?? throw new EntityNotFoundException();

            JObject submissionData = JObject.Parse(submission.Submission);

            var reportData = reportingDataGenerator.Generate(submissionData, applicationFormVersion.ReportKeys);

            submission.ReportData = reportData ?? "{}";
        }
    }
}
