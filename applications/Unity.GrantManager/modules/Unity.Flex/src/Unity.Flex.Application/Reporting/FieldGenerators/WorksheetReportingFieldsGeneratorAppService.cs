using Microsoft.AspNetCore.Authorization;
using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.Flex.Domain.Worksheets;
using Unity.Modules.Shared.Permissions;
using Volo.Abp.Application.Services;
using Volo.Abp.TenantManagement;
using Volo.Abp.Uow;

namespace Unity.Flex.Reporting.FieldGenerators
{
    [Authorize(IdentityConsts.ITAdminPolicy)]
    public class WorksheetReportingFieldsGeneratorAppService(IReportingFieldsGeneratorService<Worksheet> reportingFieldsGeneratorService,
        IWorksheetRepository worksheetRepository,
        ITenantRepository tenantRepository,
        IUnitOfWorkManager unitOfWorkManager) : ApplicationService, IWorksheetReportingFieldsGeneratorAppService
    {
        /// <summary>
        /// Generate / Update a worksheets Reporting Fields Remotely
        /// </summary>
        /// <param name="worksheetId"></param>
        /// <returns></returns>
        public async Task Generate(Guid worksheetId)
        {
            var worksheet = await worksheetRepository.GetAsync(worksheetId, true);
            reportingFieldsGeneratorService.GenerateAndSet(worksheet);
        }

        /// <summary>
        /// Sync all the worksheet reporting fields that are missing for all tenants
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
                    var worksheetIds = (await worksheetRepository.GetListAsync())
                        .Where(w => w.Published && string.IsNullOrEmpty(w.ReportViewName))
                        .Select(s => s.Id);

                    foreach (var worksheetId in worksheetIds)
                    {
                        // Generate the Reporting Fields for the worksheet
                        await Generate(worksheetId);
                    }
                    await uow.CompleteAsync();
                }
            }
        }
    }
}
