using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Flex.Domain.WorksheetInstances;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace Unity.Flex.EntityFrameworkCore.Repositories
{
    public class WorksheetInstanceRepository(IDbContextProvider<FlexDbContext> dbContextProvider) : EfCoreRepository<FlexDbContext, WorksheetInstance, Guid>(dbContextProvider), IWorksheetInstanceRepository
    {
        public async Task<WorksheetInstance?> GetByCorrelationAnchorAsync(Guid correlationId, string correlationProvider, string uiAnchor, bool includeDetails)
        {
            var dbSet = await GetDbSetAsync();

            return await dbSet.IncludeDetails()
                .FirstOrDefaultAsync(s => s.CorrelationId == correlationId
                        && s.CorrelationProvider == correlationProvider
                        && s.UiAnchor == uiAnchor);
        }

        public async Task<List<WorksheetInstance>> GetByWorksheetAnchorAsync(Guid worksheetId, string uiAnchor)
        {
            var dbSet = await GetDbSetAsync();

            return await dbSet
                .Where(s => s.WorksheetId == worksheetId
                        && s.UiAnchor == uiAnchor)
                .ToListAsync();
        }
    }
}
