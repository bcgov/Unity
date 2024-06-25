using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Flex.Domain.Services;
using Unity.Flex.Domain.WorksheetLinks;

namespace Unity.Flex.Worksheets
{
    [Authorize]
    public class WorksheetLinkAppService(WorksheetsManager worksheetsManager, IWorksheetLinkRepository worksheetLinkRepository) : FlexAppService, IWorksheetLinkAppService
    {
        public virtual async Task<WorksheetLinkDto> CreateAsync(CreateWorksheetLinkDto dto)
        {
            var worksheetLink = await worksheetsManager.CreateWorksheetLink(dto.WorksheetId, dto.CorrelationId, dto.CorrelationProvider, dto.UiAnchor);

            return ObjectMapper.Map<WorksheetLink, WorksheetLinkDto>(worksheetLink);
        }

        public virtual async Task<List<WorksheetLinkDto>> GetListByCorrelationAsync(Guid correlationId, string correlationProvider)
        {
            var worksheetLinks = await worksheetLinkRepository.GetListByCorrelationAsync(correlationId, correlationProvider);

            return ObjectMapper.Map<List<WorksheetLink>, List<WorksheetLinkDto>>(worksheetLinks);            
        }
    }
}
