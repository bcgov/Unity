using System;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace Unity.Flex.Domain.WorksheetLinks
{
    public interface IWorksheetLinkRepository : IBasicRepository<WorksheetLink, Guid>
    {
        Task<WorksheetLink?> GetExistingLinkAsync(Guid worksheetId, Guid correlationId, string correlationProvider);
    }
}
