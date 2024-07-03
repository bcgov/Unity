using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace Unity.Flex.Domain.Worksheets
{
    public interface IWorksheetListRepository : IReadOnlyRepository<Worksheet, Guid>
    {
        Task<List<Worksheet>> GetListByCorrelationAsync(Guid? correlationId, string correlationProvider, bool includeDetails = false);
        Task<Worksheet> GetAsync(Guid id, bool includeDetails = false);
        Task<List<Worksheet>> GetListAsync(bool includeDetails = false);
    }
}
