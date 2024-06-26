using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Flex.Domain.Worksheets;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace Unity.Flex.EntityFrameworkCore.Repositories
{
    public class WorksheetListRepository(IDbContextProvider<FlexDbContext> dbContextProvider) : EfCoreRepository<FlexDbContext, Worksheet, Guid>(dbContextProvider), IWorksheetListRepository
    {
        public async Task<Worksheet> GetAsync(Guid id, bool includeDetails = false)
        {
            var dbSet = await GetDbSetAsync();

            return await dbSet
                .IncludeDetails(includeDetails)
                .FirstAsync(s => s.Id == id);
        }

        public async Task<List<Worksheet>> GetListAsync(bool includeDetails = false)
        {
            var dbSet = await GetDbSetAsync();

            return await dbSet
                .IncludeDetails(includeDetails)
                .OrderBy(s => s.Name)
                .ToListAsync();
        }

        public async Task<List<Worksheet>> GetListByCorrelationAsync(Guid? correlationId, string correlationProvider, bool includeDetails = false)
        {
            var dbSet = await GetDbSetAsync();

            return await dbSet
                .IncludeDetails(includeDetails)
                .Where(s => s.Links.Any(s => s.CorrelationId == correlationId && s.CorrelationProvider == correlationProvider))
                .OrderBy(s => s.Name)
                .ToListAsync();
        }
    }
}
