using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Flex.Domain.WorksheetLinks;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace Unity.Flex.EntityFrameworkCore.Repositories
{
    public class WorksheetLinkRepository(IDbContextProvider<FlexDbContext> dbContextProvider) : EfCoreRepository<FlexDbContext, WorksheetLink, Guid>(dbContextProvider), IWorksheetLinkRepository
    {
        public async Task<List<WorksheetLink>> GetListByWorksheetAsync(Guid worksheetId, string correlationProvider)
        {
            var dbSet = await GetDbSetAsync();

            return await dbSet.Include(s => s.Worksheet)
                .Where(s => s.WorksheetId == worksheetId
                && s.CorrelationProvider == correlationProvider).ToListAsync();
        }

        public async Task<WorksheetLink?> GetExistingLinkAsync(Guid worksheetId, Guid correlationId, string correlationProvider)
        {
            var dbSet = await GetDbSetAsync();

            return await dbSet.Include(s => s.Worksheet)
                .FirstOrDefaultAsync(s => s.WorksheetId == worksheetId
                && s.CorrelationId == correlationId
                && s.CorrelationProvider == correlationProvider);
        }

        public async Task<List<WorksheetLink>> GetListByCorrelationAsync(Guid correlationId, string correlationProvider)
        {
            var dbSet = await GetDbSetAsync();

            return await dbSet.Include(s => s.Worksheet)
                .Where(s => s.CorrelationId == correlationId
                    && s.CorrelationProvider == correlationProvider)
                    .ToListAsync();
        }
    }
}
