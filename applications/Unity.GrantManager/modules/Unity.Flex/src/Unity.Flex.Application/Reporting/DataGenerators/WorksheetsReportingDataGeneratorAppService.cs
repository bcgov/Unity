using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;
using Unity.Flex.Domain.WorksheetInstances;
using Unity.Flex.Domain.Worksheets;
using Unity.Modules.Shared.Permissions;
using Volo.Abp.Application.Services;

namespace Unity.Flex.Reporting.DataGenerators
{
    [Authorize(IdentityConsts.ITAdminPolicy)]
    public class WorksheetsReportingDataGeneratorAppService(IReportingDataGeneratorService<Worksheet, WorksheetInstance> reportingDataGeneratorService,
        IWorksheetInstanceRepository worksheetInstanceRepository,
        IWorksheetRepository worksheetRepository)
        : ApplicationService, IWorksheetReportingDataGeneratorAppService
    {
        public async Task Generate(Guid worksheetInstanceId)
        {
            var worksheetInstance = await worksheetInstanceRepository.GetAsync(worksheetInstanceId, true);
            var worksheet = await worksheetRepository.GetAsync(worksheetInstance.WorksheetId);
            reportingDataGeneratorService.GenerateAndSet(worksheet, worksheetInstance);
        }
    }
}
