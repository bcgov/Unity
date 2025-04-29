using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Reporting.DataGenerators;
using Unity.GrantManager.Reporting.FieldGenerators;
using Unity.Modules.Shared.Permissions;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Features; 
using Volo.Abp.TenantManagement;
using Volo.Abp.Uow;

namespace Unity.GrantManager.Reporting
{
    [Authorize(IdentityConsts.ITAdminPolicyName)]
    public class FormsReportSyncServiceAppService(IReportingFieldsGeneratorService reportingFieldsGeneratorService,
        IApplicationFormVersionRepository applicationFormVersionRepository,
        IReportingDataGenerator reportingDataGenerator,
        IUnitOfWorkManager unitOfWorkManager,
        ITenantRepository tenantRepository,
        IApplicationFormSubmissionRepository applicationFormSubmissionRepository,
        IFeatureChecker featureChecker)
        : ApplicationService, IFormsReportSyncServiceAppService
    {
        /// <summary>
        /// Generate the reporting fields for a form version
        /// </summary>
        /// <param name="formVersionId"></param>
        /// <returns></returns>
        public async Task GenerateFormVersionFields(Guid formVersionId)
        {
            var applicationFormVersion = await applicationFormVersionRepository.GetAsync(formVersionId);
            await reportingFieldsGeneratorService.GenerateAndSetAsync(applicationFormVersion);
        }

        /// <summary>
        /// Generate the form submission data for a form version
        /// </summary>
        /// <param name="submissionId"></param>
        /// <returns></returns>
        /// <exception cref="EntityNotFoundException"></exception>
        public async Task GenerateFormSubmissionData(Guid submissionId)
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

            var reportData = reportingDataGenerator.Generate(submissionData, applicationFormVersion.ReportKeys, submissionId);

            submission.ReportData = reportData ?? "{}";
        }

        /// <summary>
        /// Sync / Generate all the form version reporting fields that are missing for all tenants
        /// </summary>
        /// <returns></returns>
        public async Task SyncFormVersionFields()
        {
            var tenants = await tenantRepository.GetListAsync();

            foreach (var tenant in tenants)
            {
                using (CurrentTenant.Change(tenant.Id))
                {
                    if (await featureChecker.IsEnabledAsync("Unity.Reporting"))
                    {
                        using var uow = unitOfWorkManager.Begin(isTransactional: false);
                        // Get all the Form Version thats have no report data associated
                        var formVersionIds = (await applicationFormVersionRepository.GetListAsync())
                            .Where(w => string.IsNullOrEmpty(w.ReportViewName))
                        .Select(s => s.Id);

                        foreach (var formVersionId in formVersionIds)
                        {
                            // Generate the Reporting Fields for the form version
                            await GenerateFormVersionFields(formVersionId);
                        }

                        await uow.CompleteAsync();
                    }
                }
            }
        }

        /// <summary>
        /// Sync / Generate all the form submission reporting data that are missing for all tenants
        /// </summary>
        /// <returns></returns>
        public async Task SyncFormSubmissionData()
        {
            var tenants = await tenantRepository.GetListAsync();

            foreach (var tenant in tenants)
            {
                using (CurrentTenant.Change(tenant.Id))
                {
                    if (await featureChecker.IsEnabledAsync("Unity.Reporting"))
                    {
                        using var uow = unitOfWorkManager.Begin(isTransactional: false);

                        var submissionIds = (await applicationFormSubmissionRepository
                           .GetListAsync())
                           .Where(s => string.IsNullOrEmpty(s.ReportData) || s.ReportData == "{}")
                        .Select(s => s.Id);

                        foreach (var submissionId in submissionIds)
                        {
                            // Generate the Reporting Fields for the form version
                            await GenerateFormSubmissionData(submissionId);
                        }

                        await uow.CompleteAsync();
                    }
                }
            }
        }
    }
}
