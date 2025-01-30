using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;
using Unity.Flex.Domain.Worksheets;
using Volo.Abp.Application.Services;

namespace Unity.Flex.Reporting.FieldGenerators
{
    [Authorize]
    public class WorksheetReportingFieldsGeneratorAppService(IReportingFieldsGeneratorService<Worksheet> reportingFieldsGeneratorService,
        IWorksheetRepository worksheetRepository) : ApplicationService, IWorksheetReportingFieldsGeneratorAppService
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
    }
}
