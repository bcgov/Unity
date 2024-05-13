using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using Unity.Flex.Domain.WorksheetInstances;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace Unity.Flex.EntityFrameworkCore.Repositories
{
    public class WorksheetInstanceRepository(IDbContextProvider<FlexDbContext> dbContextProvider) : EfCoreRepository<FlexDbContext, WorksheetInstance, Guid>(dbContextProvider), IWorksheetInstanceRepository
    {
        public async Task<WorksheetInstance?> GetByCorrelationAsync(Guid correlationId, string correlationProvider, string correlationAnchor, bool includeDetails)
        {
            var dbSet = await GetDbSetAsync();

            return await dbSet.IncludeDetails()
                .FirstOrDefaultAsync(s => s.CorrelationId == correlationId
                        && s.CorrelationProvider == correlationProvider
                        && s.CorrelationAnchor == correlationAnchor);
        }
    }
}
