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
        public async Task<List<Worksheet>> GetListByCorrelationAsync(Guid? correlationId, string correlationProvider)
        {
            var dbSet = await GetDbSetAsync();

            return await dbSet
                .Where(s => s.Links.Any(s => s.CorrelationId == correlationId && s.CorrelationProvider == correlationProvider))
                .OrderBy(s => s.Name)
                .ToListAsync();
        }
    }
}
