using System;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Unity.Flex.Domain.ScoresheetInstances;

namespace Unity.Flex.EntityFrameworkCore.Repositories
{
    public class ScoresheetInstanceRepository(IDbContextProvider<FlexDbContext> dbContextProvider) : EfCoreRepository<FlexDbContext, ScoresheetInstance, Guid>(dbContextProvider), IScoresheetInstanceRepository
    {
        public async Task<ScoresheetInstance?> GetByCorrelationAsync(Guid correlationId)
        {
            var dbContext = await GetDbContextAsync();
            return await dbContext.ScoresheetInstances
                    .Include(s => s.Answers)
                    .FirstOrDefaultAsync(s => s.CorrelationId == correlationId);
        }

        public async Task<ScoresheetInstance?> GetWithAnswersAsync(Guid scoresheetInstanceId)
        {
            var dbSet = await GetDbSetAsync();

            return await dbSet
            .Include(wi => wi.Answers)
                .FirstOrDefaultAsync(si => si.Id == scoresheetInstanceId);
        }
    }
}
