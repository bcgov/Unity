using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace Unity.Flex.Domain.WorksheetInstances
{
    public interface IWorksheetInstanceRepository : IBasicRepository<WorksheetInstance, Guid>
    {
        Task<WorksheetInstance?> GetByCorrelationAnchorWorksheetAsync(Guid correlationId, string correlationProvider, Guid worksheetId, string uiAnchor, bool includeDetails);
        Task<List<WorksheetInstance>> GetByWorksheetCorrelationAsync(Guid worksheetId, string uiAnchor, Guid worksheetCorrelationId, string worksheetCorrelationProvider);
        Task<WorksheetInstance?> GetWithValuesAsync(Guid worksheetInstanceId);
        Task<bool> ExistsAsync(Guid worksheetId, Guid instanceCorrelationId, string instanceCorrelationProvider, Guid sheetCorrelationId, string sheetCorrelationProvider, string? uiAnchor);
        Task<bool> AnyByWorksheetAndFormVersionAsync(Guid worksheetId, Guid formVersionId);
        Task<List<WorksheetInstance>> GetByCorrelationIdsAsync(IEnumerable<Guid> correlationIds, string correlationProvider);
        Task<List<Guid>> GetDistinctWorksheetIdsByCorrelationProviderAsync(string correlationProvider);
        Task<List<Guid>> GetDistinctWorksheetIdsByCorrelationIdsAsync(IEnumerable<Guid> correlationIds, string correlationProvider);
        Task<List<WorksheetInstance>> GetPagedListByCorrelationProviderAsync(string correlationProvider, int skipCount, int maxResultCount);
        Task<int> GetCountByCorrelationProviderAsync(string correlationProvider);
    }
}
