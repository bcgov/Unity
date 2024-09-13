using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace Unity.Flex.Domain.WorksheetLinks
{
    public interface IWorksheetLinkRepository : IBasicRepository<WorksheetLink, Guid>
    {
        Task<List<WorksheetLink>> GetListByWorksheetAsync(Guid worksheetId, string correlationProvider);
        Task<WorksheetLink?> GetExistingLinkAsync(Guid worksheetId, Guid correlationId, string correlationProvider);
        Task<List<WorksheetLink>> GetListByCorrelationAsync(Guid correlationId, string correlationProvider);        
    }
}
