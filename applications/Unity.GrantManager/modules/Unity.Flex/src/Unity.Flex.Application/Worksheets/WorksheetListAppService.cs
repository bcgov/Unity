using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Unity.Flex.Domain.Worksheets;
using Microsoft.AspNetCore.Authorization;

namespace Unity.Flex.Worksheets
{
    [Authorize]
    public class WorksheetListAppService(IWorksheetListRepository worksheetListRepository) : FlexAppService, IWorksheetListAppService
    {
        public virtual async Task<WorksheetBasicDto> GetAsync(Guid id)
        {
            return ObjectMapper.Map<Worksheet, WorksheetBasicDto>(await worksheetListRepository.GetAsync(id, true));
        }

        public virtual async Task<List<WorksheetBasicDto>> GetListAsync()
        {
            return ObjectMapper.Map<List<Worksheet>, List<WorksheetBasicDto>>(await worksheetListRepository.GetListAsync(true));
        }

        public virtual async Task<List<WorksheetBasicDto>> GetListByCorrelationAsync(Guid correlationId, string correlationProvider)
        {
            var worksheets = await worksheetListRepository.GetListByCorrelationAsync(correlationId, correlationProvider);

            return ObjectMapper.Map<List<Worksheet>, List<WorksheetBasicDto>>(worksheets);
        }
    }
}
