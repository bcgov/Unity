using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using Unity.Flex.Domain.WorksheetLinks;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace Unity.Flex.EntityFrameworkCore.Repositories
{
    public class WorksheetLinkRepository(IDbContextProvider<FlexDbContext> dbContextProvider) : EfCoreRepository<FlexDbContext, WorksheetLink, Guid>(dbContextProvider), IWorksheetLinkRepository
    {
        public async Task<WorksheetLink?> GetExistingLinkAsync(Guid worksheetId, Guid correlationId, string correlationProvider)
        {
            var dbSet = await GetDbSetAsync();

            return await dbSet.FirstOrDefaultAsync(s => s.WorksheetId == worksheetId
                && s.CorrelationId == correlationId
                && s.CorrelationProvider == correlationProvider);
        }
    }
}
