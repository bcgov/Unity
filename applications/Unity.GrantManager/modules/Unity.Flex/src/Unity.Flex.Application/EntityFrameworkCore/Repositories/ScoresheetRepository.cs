using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Flex.Domain.Scoresheets;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace Unity.Flex.EntityFrameworkCore.Repositories
{
    public class ScoresheetRepository(IDbContextProvider<FlexDbContext> dbContextProvider) : EfCoreRepository<FlexDbContext, Scoresheet, Guid>(dbContextProvider), IScoresheetRepository
    {
        public async Task<Scoresheet?> GetHighestVersionAsync(Guid groupId)
        {
            var dbContext = await GetDbContextAsync();
            return await dbContext.Scoresheets
                .Where(s => s.GroupId == groupId)
                .OrderByDescending(s => s.Version)
                .FirstOrDefaultAsync();
        }

        public async Task<List<Scoresheet>> GetListWithChildrenAsync()
        {
            var dbContext = await GetDbContextAsync();
            return await dbContext.Scoresheets
                .Include(s => s.Sections.OrderBy(sec => sec.Order))
                .ThenInclude(sec => sec.Fields.OrderBy(q => q.Order))
                .OrderBy(s => s.CreationTime)
                .ToListAsync();    
        }

        public async Task<List<Scoresheet>> GetScoresheetsByGroupId(Guid groupId)
        {
            return (await GetListWithChildrenAsync()).Where(s => s.GroupId == groupId).ToList();
        }

        public async Task<Scoresheet?> GetWithChildrenAsync(Guid id)
        {
            var dbContext = await GetDbContextAsync();
            return await dbContext.Scoresheets
                    .Include(s => s.Sections)
                    .ThenInclude(ss => ss.Fields)
                    .FirstOrDefaultAsync(s => s.Id == id);
        }
    }
}
