using Microsoft.AspNetCore.Authorization;
using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.Flex.Domain.ScoresheetInstances;
using Unity.Flex.Domain.Scoresheets;
using Unity.Flex.Reporting.DataGenerators;
using Unity.Flex.Reporting.FieldGenerators;
using Unity.Modules.Shared.Permissions;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Features;
using Volo.Abp.TenantManagement;
using Volo.Abp.Uow;

namespace Unity.Flex.Reporting
{
    [Authorize(IdentityConsts.ITAdminPolicyName)]
    public class ScoresheetReportingFieldsSyncAppService(IReportingFieldsGeneratorService<Scoresheet> reportingFieldsGeneratorService,
        IReportingDataGeneratorService<Scoresheet, ScoresheetInstance> reportingFieldsDataGeneratorService,
        IScoresheetInstanceRepository scoresheetInstanceRepository,
        IScoresheetRepository scoresheetRepository,
        ITenantRepository tenantRepository,
        IFeatureChecker featureChecker,
        IUnitOfWorkManager unitOfWorkManager) : ApplicationService, IScoresheetReportingFieldsSyncAppService
    {
        /// <summary>
        /// Generate / Update a scoresheets Reporting Fields Remotely
        /// </summary>
        /// <param name="scoresheetId"></param>
        /// <returns></returns>
        public async Task GenerateQuestions(Guid scoresheetId)
        {
            var scoresheet = await scoresheetRepository.GetAsync(scoresheetId, true);
            reportingFieldsGeneratorService.GenerateAndSet(scoresheet);
        }

        /// <summary>
        /// Generate / Update a scoresheet instance Reporting Data Remotely
        /// </summary>
        /// <param name="scoresheetInstanceId"></param>
        /// <returns></returns>
        /// <exception cref="EntityNotFoundException"></exception>
        public async Task GenerateAnswers(Guid scoresheetInstanceId)
        {
            var scoresheetInstance = await scoresheetInstanceRepository.GetWithAnswersAsync(scoresheetInstanceId) ?? throw new EntityNotFoundException();
            var scoresheet = await scoresheetRepository.GetAsync(scoresheetInstance.ScoresheetId);
            reportingFieldsDataGeneratorService.GenerateAndSet(scoresheet, scoresheetInstance);
        }

        /// <summary>
        /// Sync all the scoresheet reporting fields that are missing for all tenants
        /// </summary>
        /// <returns></returns>
        public async Task SyncQuestions()
        {
            var tenants = await tenantRepository.GetListAsync();

            foreach (var tenant in tenants)
            {
                using (CurrentTenant.Change(tenant.Id))
                {
                    if (await featureChecker.IsEnabledAsync("Unity.Reporting"))
                    {
                        using var uow = unitOfWorkManager.Begin(isTransactional: false);
                        var scoresheetIds = (await scoresheetRepository.GetListAsync())
                            .Where(w => w.Published && string.IsNullOrEmpty(w.ReportViewName))
                            .Select(s => s.Id);

                        foreach (var scoresheetId in scoresheetIds)
                        {
                            // Generate the Reporting Fields for the worksheet
                            await GenerateQuestions(scoresheetId);
                        }
                        await uow.CompleteAsync();
                    }
                }
            }
        }

        /// <summary>
        /// Sync all the scoresheet reporting data that are missing for all tenants
        /// </summary>
        /// <returns></returns>
        public async Task SyncAnswers()
        {
            var tenants = await tenantRepository.GetListAsync();

            foreach (var tenant in tenants)
            {
                using (CurrentTenant.Change(tenant.Id))
                {
                    if (await featureChecker.IsEnabledAsync("Unity.Reporting"))
                    {
                        using var uow = unitOfWorkManager.Begin(isTransactional: false);
                        var scoresheetInstanceIds = (await scoresheetInstanceRepository
                            .GetListAsync())
                            .Where(s => string.IsNullOrEmpty(s.ReportData) || s.ReportData == "{}")
                            .Select(s => s.Id);

                        foreach (var scoresheetInstanceId in scoresheetInstanceIds)
                        {
                            // Generate the Reporting Fields for the worksheet
                            await GenerateAnswers(scoresheetInstanceId);
                        }
                        await uow.CompleteAsync();
                    }
                }
            }
        }
    }
}
