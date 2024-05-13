using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace Unity.Flex.Domain.Worksheets
{
    public interface IWorksheetRepository : IBasicRepository<Worksheet, Guid>
    {
        Task<Worksheet?> GetBySectionAsync(Guid id, bool includeDetails = false);
        Task<List<Worksheet>> GetListOrderedAsync(bool includeDetails = false);
    }
}
