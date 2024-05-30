using System;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace Unity.Flex.Domain.WorksheetInstances
{
    public interface IWorksheetInstanceRepository : IBasicRepository<WorksheetInstance, Guid>
    {
        Task<WorksheetInstance?> GetByCorrelationByAnchorAsync(Guid correlationId, string correlationProvider, string correlationAnchor, bool includeDetails);
    }
}
