using Microsoft.AspNetCore.Authorization;
using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.Flex.Domain.WorksheetInstances;
using Unity.Flex.Domain.Worksheets;
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
    public class WorksheetReportingFieldsSyncAppService(IReportingFieldsGeneratorService<Worksheet> reportingFieldsGeneratorService,
        IReportingDataGeneratorService<Worksheet, WorksheetInstance> reportingDataGeneratorService,
        IWorksheetRepository worksheetRepository,
        IWorksheetInstanceRepository worksheetInstanceRepository,
        ITenantRepository tenantRepository,
        IFeatureChecker featureChecker,
        IUnitOfWorkManager unitOfWorkManager) : ApplicationService, IWorksheetReportingFieldsSyncAppService
    {
        const string ReportingFeatureName = "Unity.Reporting";

        /// <summary>
        /// Generate / Update a worksheets Reporting Fields Remotely
        /// </summary>
        /// <param name="worksheetId"></param>
        /// <returns></returns>
        private async Task GenerateFields(Guid worksheetId)
        {
            var worksheet = await worksheetRepository.GetAsync(worksheetId, true);
            reportingFieldsGeneratorService.GenerateAndSet(worksheet);
        }

        /// <summary>
        /// Generate / Update a worksheet instance Reporting Data Remotely
        /// </summary>
        /// <param name="worksheetInstanceId"></param>
        /// <returns></returns>
        private async Task GenerateData(Guid worksheetInstanceId)
        {
            var worksheetInstance = await worksheetInstanceRepository.GetWithValuesAsync(worksheetInstanceId) ?? throw new EntityNotFoundException();
            var worksheet = await worksheetRepository.GetAsync(worksheetInstance.WorksheetId);
            reportingDataGeneratorService.GenerateAndSet(worksheet, worksheetInstance);
        }

        /// <summary>
        /// Sync all the worksheet reporting fields that are missing either for all tenants or specified tenant
        /// </summary>
        /// <returns></returns>
        public async Task SyncFields(Guid? tenantId)
        {
            // Sync the fields for the current tenant if specified
            if (tenantId.HasValue)
            {
                using (CurrentTenant.Change(tenantId.Value))
                {
                    if (await featureChecker.IsEnabledAsync(ReportingFeatureName))
                    {
                        await SyncFieldsForCurrentTenant();
                    }
                }
                return;
            }

            // Sync the fields for all tenants
            var tenants = await tenantRepository.GetListAsync();

            foreach (var tenant in tenants)
            {
                using (CurrentTenant.Change(tenant.Id))
                {
                    if (await featureChecker.IsEnabledAsync(ReportingFeatureName))
                    {
                        await SyncFieldsForCurrentTenant();
                    }
                }
            }
        }

        /// <summary>
        /// Sync the fields for the current tenant context
        /// </summary>
        /// <returns></returns>
        private async Task SyncFieldsForCurrentTenant()
        {
            using var uow = unitOfWorkManager.Begin(isTransactional: false);
            var worksheetIds = (await worksheetRepository.GetListAsync())
                .Where(w => w.Published && string.IsNullOrEmpty(w.ReportViewName))
                .Select(s => s.Id);

            foreach (var worksheetId in worksheetIds)
            {
                // Generate the Reporting Fields for the worksheet
                await GenerateFields(worksheetId);
            }
            await uow.CompleteAsync();
        }

        /// <summary>
        /// Sync all the worksheet reporting data that are missing for all tenants or specified tenant
        /// </summary>
        /// <returns></returns>
        public async Task SyncData(Guid? tenantId)
        {
            // Sync the data for the current tenant if specified
            if (tenantId.HasValue)
            {
                using (CurrentTenant.Change(tenantId.Value))
                {
                    if (await featureChecker.IsEnabledAsync(ReportingFeatureName))
                    {
                        await SyncDataForCurrentTenant();
                    }
                }
                return;
            }

            // Sync the data for all tenants
            var tenants = await tenantRepository.GetListAsync();

            foreach (var tenant in tenants)
            {
                using (CurrentTenant.Change(tenant.Id))
                {
                    if (await featureChecker.IsEnabledAsync(ReportingFeatureName))
                    {
                        await SyncDataForCurrentTenant();
                    }
                }
            }
        }

        /// <summary>
        /// Sync the data for the current tenant context
        /// </summary>
        /// <returns></returns>
        private async Task SyncDataForCurrentTenant()
        {
            using var uow = unitOfWorkManager.Begin(isTransactional: false);
            var worksheetInstanceIds = (await worksheetInstanceRepository
                .GetListAsync())
                .Select(s => s.Id);

            foreach (var worksheetInstanceId in worksheetInstanceIds)
            {
                // Generate the Reporting Fields for the worksheet
                await GenerateData(worksheetInstanceId);
            }

            await uow.CompleteAsync();
        }
    }
}
