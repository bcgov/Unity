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
    public class WorksheetRepository(IDbContextProvider<FlexDbContext> dbContextProvider) : EfCoreRepository<FlexDbContext, Worksheet, Guid>(dbContextProvider), IWorksheetRepository
    {
        public async Task<Worksheet?> GetBySectionAsync(Guid id, bool includeDetails = false)
        {
            var dbSet = await GetDbSetAsync();

            return await dbSet.IncludeDetails(includeDetails)
                .FirstOrDefaultAsync(s => s.Sections.Any(s => s.Id == id));
        }

        public async Task<List<Worksheet>> GetListOrderedAsync(bool includeDetails = false)
        {
            var dbSet = await GetDbSetAsync();

            return await dbSet.IncludeDetails(includeDetails).OrderBy(s => s.Name).ToListAsync();
        }
    }
}
