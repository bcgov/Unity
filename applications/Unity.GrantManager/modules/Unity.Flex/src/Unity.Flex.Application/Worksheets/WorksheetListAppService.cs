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
        public async Task<List<WorksheetBasicDto>> GetListAsync()
        {
            return ObjectMapper.Map<List<Worksheet>, List<WorksheetBasicDto>>(await worksheetListRepository.GetListAsync());
        }

        public virtual async Task<List<WorksheetBasicDto>> GetListByCorrelationAsync(Guid? correlationId, string correlationProvider)
        {
            var worksheets = await worksheetListRepository.GetListByCorrelationAsync(correlationId, correlationProvider);

            return ObjectMapper.Map<List<Worksheet>, List<WorksheetBasicDto>>(worksheets);
        }
    }
}
