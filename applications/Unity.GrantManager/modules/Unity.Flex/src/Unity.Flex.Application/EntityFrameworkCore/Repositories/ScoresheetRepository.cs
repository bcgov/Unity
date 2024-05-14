using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Unity.Flex.Domain.Scoresheets;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace Unity.Flex.EntityFrameworkCore.Repositories
{
    public class ScoresheetRepository(IDbContextProvider<FlexDbContext> dbContextProvider) : EfCoreRepository<FlexDbContext, Scoresheet, Guid>(dbContextProvider), IScoresheetRepository
    {
        public async Task<List<Scoresheet>> GetListWithChildrenAsync()
        {
            var dbContext = await GetDbContextAsync();
            return await dbContext.Scoresheets
                .Include(s => s.Sections)
                .ThenInclude(ss => ss.Fields)
                .OrderBy(s => s.CreationTime)
                .ToListAsync();    
        }
    }
}
