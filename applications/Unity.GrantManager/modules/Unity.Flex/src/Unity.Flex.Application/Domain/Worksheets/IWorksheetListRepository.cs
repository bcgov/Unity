using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace Unity.Flex.Domain.Worksheets
{
    public interface IWorksheetListRepository : IReadOnlyRepository<Worksheet, Guid>
    {
        Task<List<Worksheet>> GetListByCorrelationAsync(Guid? correlationId, string correlationProvider);
    }
}
