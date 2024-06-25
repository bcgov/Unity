using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.Flex.Worksheets
{
    public interface IWorksheetListAppService : IApplicationService
    {
        Task<WorksheetBasicDto> GetAsync(Guid id);
        Task<List<WorksheetBasicDto>> GetListByCorrelationAsync(Guid correlationId, string correlationProvider);
        Task<List<WorksheetBasicDto>> GetListAsync();
    }
}
