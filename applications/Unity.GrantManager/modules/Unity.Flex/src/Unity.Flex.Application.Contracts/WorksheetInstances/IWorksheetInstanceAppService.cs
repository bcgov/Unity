using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace Unity.Flex.WorksheetInstances
{
    public interface IWorksheetInstanceAppService : IApplicationService
    {
        Task<WorksheetInstanceDto> GetByCorrelationAnchorAsync(Guid correlationId, string correlationProvider, Guid worksheetId, string uiAnchor);
        Task<WorksheetInstanceDto> CreateAsync(CreateWorksheetInstanceDto dto);
        Task UpdateAsync(PersistWorksheetIntanceValuesDto dto);
        Task<List<WorksheetInstanceDataDto>> GetListByCorrelationIdsAsync(List<Guid> correlationIds, string correlationProvider);
        Task<List<Guid>> GetDistinctWorksheetIdsByCorrelationProviderAsync(string correlationProvider);
        Task<List<Guid>> GetDistinctWorksheetIdsByCorrelationIdsAsync(List<Guid> correlationIds, string correlationProvider);
        Task<PagedResultDto<WorksheetInstanceDataDto>> GetPagedListByCorrelationProviderAsync(string correlationProvider, int skipCount, int maxResultCount);
        Task<WorksheetInstanceDataDto?> GetDataByIdAsync(Guid id);
    }
}
