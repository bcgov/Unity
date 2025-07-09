using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace Unity.Flex.Domain.WorksheetInstances
{
    public interface IWorksheetInstanceRepository : IBasicRepository<WorksheetInstance, Guid>
    {
        Task<WorksheetInstance?> GetByCorrelationAnchorWorksheetAsync(Guid correlationId, string correlationProvider, Guid worksheetId, string uiAnchor, bool includeDetails);
        Task<WorksheetInstance?> GetByCorrelationAnchorAsync(Guid correlationId, string correlationProvider, string uiAnchor, bool includeDetails);        
        Task<List<WorksheetInstance>> GetByWorksheetCorrelationAsync(Guid worksheetId, string uiAnchor, Guid worksheetCorrelationId, string worksheetCorrelationProvider);
        Task<WorksheetInstance?> GetWithValuesAsync(Guid worksheetInstanceId);
        Task<bool> GetExistingAsync(Guid worksheetId, Guid instanceCorrelationId, string instanceCorrelationProvider, Guid sheetCorrelationId, string sheetCorrelationProvider, string? uiAnchor);
    }
}
