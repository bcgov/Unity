using Microsoft.AspNetCore.Authorization;
using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.Flex.Domain.WorksheetInstances;
using Unity.Flex.Domain.Worksheets;
using Unity.Modules.Shared.Permissions;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Entities;
using Volo.Abp.TenantManagement;
using Volo.Abp.Uow;

namespace Unity.Flex.Reporting.DataGenerators
{
    [Authorize(IdentityConsts.ITAdminPolicy)]
    public class WorksheetsReportingDataGeneratorAppService(IReportingDataGeneratorService<Worksheet, WorksheetInstance> reportingDataGeneratorService,
        IWorksheetInstanceRepository worksheetInstanceRepository,
        IWorksheetRepository worksheetRepository,
        ITenantRepository tenantRepository,
        IUnitOfWorkManager unitOfWorkManager)
        : ApplicationService, IWorksheetReportingDataGeneratorAppService
    {
        /// <summary>
        /// Generate / Update a worksheet instance Reporting Data Remotely
        /// </summary>
        /// <param name="worksheetInstanceId"></param>
        /// <returns></returns>
        public async Task Generate(Guid worksheetInstanceId)
        {
            var worksheetInstance = await worksheetInstanceRepository.GetWithValuesAsync(worksheetInstanceId) ?? throw new EntityNotFoundException();
            var worksheet = await worksheetRepository.GetAsync(worksheetInstance.WorksheetId);
            reportingDataGeneratorService.GenerateAndSet(worksheet, worksheetInstance);
        }

        /// <summary>
        /// Sync all the worksheet reporting data that are missing for all tenants
        /// </summary>
        /// <returns></returns>
        public async Task Sync()
        {
            var tenants = await tenantRepository.GetListAsync();

            foreach (var tenant in tenants)
            {
                using (CurrentTenant.Change(tenant.Id))
                {
                    using var uow = unitOfWorkManager.Begin(isTransactional: false);
                    var worksheetInstanceIds = (await worksheetInstanceRepository
                        .GetListAsync())
                        .Where(s => string.IsNullOrEmpty(s.ReportData) || s.ReportData == "{}")
                        .Select(s => s.Id);

                    foreach (var worksheetInstanceId in worksheetInstanceIds)
                    {
                        // Generate the Reporting Fields for the worksheet
                        await Generate(worksheetInstanceId);
                    }
                    await uow.CompleteAsync();
                }
            }
        }
    }
}
