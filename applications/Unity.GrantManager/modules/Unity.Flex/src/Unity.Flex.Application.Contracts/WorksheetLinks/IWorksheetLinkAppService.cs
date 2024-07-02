using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.Flex.WorksheetLinks
{
    public interface IWorksheetLinkAppService : IApplicationService
    {
        Task<WorksheetLinkDto> CreateAsync(CreateWorksheetLinkDto dto);
        Task<List<WorksheetLinkDto>> UpdateWorksheetLinksAsync(Guid correlationId, string correlationProvider, UpdateWorksheetLinksDto dto);
        Task<List<WorksheetLinkDto>> GetListByCorrelationAsync(Guid correlationId, string correlationProvider);
        Task<List<WorksheetLinkDto>> GetListByWorksheetAsync(Guid worksheetId, string correlationProvider);
    }
}
