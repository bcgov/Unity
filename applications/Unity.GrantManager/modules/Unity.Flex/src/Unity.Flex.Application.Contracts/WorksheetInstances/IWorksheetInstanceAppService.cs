using System.Threading.Tasks;
using System;
using Volo.Abp.Application.Services;

namespace Unity.Flex.WorksheetInstances
{
    public interface IWorksheetInstanceAppService : IApplicationService
    {
        Task<WorksheetInstanceDto> GetByCorrelationAnchorAsync(Guid correlationId, string correlationProvider, Guid? worksheetId, string uiAnchor);
        Task<WorksheetInstanceDto> CreateAsync(CreateWorksheetInstanceDto dto);
        Task UpdateAsync(PersistWorksheetIntanceValuesDto dto);
    }
}
