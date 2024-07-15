using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace Unity.Flex.Domain.Worksheets
{
    public interface IWorksheetRepository : IBasicRepository<Worksheet, Guid>
    {
        Task<Worksheet> GetAsync(Guid id, bool includeDetails = true);
        Task<List<Worksheet>> GetListAsync(bool includeDetails = false);
        Task<Worksheet?> GetByCorrelationAnchorAsync(Guid correlationId, string correlationProvider, string uiAnchor, bool includeDetails = false);
        Task<Worksheet?> GetByCorrelationByNameAsync(Guid correlationId, string correlationProvider, string name, bool includeDetails = false);
        Task<Worksheet?> GetByNameAsync(string name, bool includeDetails = false);
        Task<Worksheet?> GetBySectionAsync(Guid id, bool includeDetails = false);
        Task<List<Worksheet>> GetListOrderedAsync(Guid correlationId, string correlationProvider, bool includeDetails = false);        
        Task<List<Worksheet>> GetByNameStartsWithAsync(string name, bool includeDetails = false);
    }
}
