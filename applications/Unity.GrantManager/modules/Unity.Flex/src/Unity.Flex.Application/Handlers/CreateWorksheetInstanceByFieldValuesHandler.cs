using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Flex.Domain.Services;
using Unity.Flex.Domain.WorksheetInstances;
using Unity.Flex.Domain.Worksheets;
using Unity.Flex.WorksheetInstances;
using Unity.Flex.Worksheets.Collectors;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;
using Volo.Abp.MultiTenancy;

namespace Unity.Flex.Handlers
{
    /// <summary>
    /// Handles creating worksheet instances from field values.
    /// 
    /// Tenant context handling:
    /// - If the request comes through a route with {__tenant} parameter, ABP automatically sets the tenant context
    /// - If the request is processed via an ABP background job, tenant context is preserved automatically
    /// - If TenantId is explicitly set on the CreateWorksheetInstanceByFieldValuesEto, it will be used when current tenant is null
    /// </summary>
    public class CreateWorksheetInstanceByFieldValuesHandler(WorksheetsManager worksheetsManager,
        IServiceProvider serviceProvider,
        ICurrentTenant currentTenant)
        : ILocalEventHandler<CreateWorksheetInstanceByFieldValuesEto>, ITransientDependency
    {
        public async Task HandleEventAsync(CreateWorksheetInstanceByFieldValuesEto eventData)
        {
            // If current tenant is null but eventData provides a TenantId, use it
            if (currentTenant.Id == null && eventData.TenantId.HasValue)
            {
                using (currentTenant.Change(eventData.TenantId))
                {
                    await ProcessWorksheetInstancesAsync(eventData);
                }
            }
            else
            {
                // Continue with current tenant context (could be null/host or already set)
                await ProcessWorksheetInstancesAsync(eventData);
            }
        }

        private async Task ProcessWorksheetInstancesAsync(CreateWorksheetInstanceByFieldValuesEto eventData)
        {
            List<(Worksheet worksheet, WorksheetInstance worksheetIntance)> workSheetInstancePairs = 
                await worksheetsManager.CreateWorksheetDataByFields(eventData);

            foreach (var (worksheet, worksheetIntance) in workSheetInstancePairs.Where(s => s.worksheet.RequiresCollection()))
            {
                await worksheetIntance.CollectAsync(worksheet, serviceProvider);
            }
        }
    }
}
