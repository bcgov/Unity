using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using Unity.Flex.Domain.Services;
using Unity.Flex.Domain.WorksheetLinks;

namespace Unity.Flex.Worksheets
{
    [Authorize]
    public class WorksheetLinkAppService(WorksheetsManager worksheetsManager) : FlexAppService, IWorksheetLinkAppService
    {
        public async Task<WorksheetLinkDto> CreateAsync(CreateWorksheetLinkDto dto)
        {
            var worksheetLink = await worksheetsManager.CreateWorksheetLink(dto.WorksheetId, dto.CorrelationId, dto.CorrelationProvider);

            return ObjectMapper.Map<WorksheetLink, WorksheetLinkDto>(worksheetLink);
        }
    }
}
