using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Flex.Domain.Scoresheets;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace Unity.Flex.EntityFrameworkCore.Repositories
{
    public class ScoresheetRepository(IDbContextProvider<FlexDbContext> dbContextProvider) : EfCoreRepository<FlexDbContext, Scoresheet, Guid>(dbContextProvider), IScoresheetRepository
    {
        public async Task<Scoresheet> GetAsync(Guid id, bool includeDetails = true)
        {
            var dbSet = await GetDbSetAsync();

            return await dbSet
                .IncludeDetails(includeDetails)
                .FirstAsync(s => s.Id == id);
        }

        public async Task<Scoresheet?> GetByNameAsync(string name, bool includeDetails = false)
        {
            var dbSet = await GetDbSetAsync();

            return await dbSet
                .IncludeDetails(includeDetails)
                .FirstOrDefaultAsync(s => s.Name == name);
        }

        public async Task<List<Scoresheet>> GetListWithChildrenAsync()
        {
            var dbContext = await GetDbContextAsync();
            return await dbContext.Scoresheets
                .Include(s => s.Sections.OrderBy(sec => sec.Order))
                .ThenInclude(sec => sec.Fields.OrderBy(q => q.Order))
                .OrderBy(s => s.Order)
                .ThenBy(s => s.CreationTime)
                .ToListAsync();
        }

        public async Task<List<Scoresheet>> GetPublishedListAsync()
        {
            var dbContext = await GetDbContextAsync();
            return await dbContext.Scoresheets
                .Where(scoresheet => scoresheet.Published)
                .ToListAsync();
        }

        public async Task<Scoresheet?> GetWithChildrenAsync(Guid id)
        {
            var dbContext = await GetDbContextAsync();
            return await dbContext.Scoresheets
                    .Include(s => s.Sections.OrderBy(sec => sec.Order))
                    .ThenInclude(ss => ss.Fields.OrderBy(q => q.Order))
                    .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<Scoresheet?> GetBySectionAsync(Guid id, bool includeDetails = false)
        {
            var dbSet = await GetDbSetAsync();

            return await dbSet
                .IncludeDetails(includeDetails)
                .FirstOrDefaultAsync(s => s.Sections.Any(s => s.Id == id));
        }

        public async Task<List<Scoresheet>> GetByNameStartsWithAsync(string name, bool includeDetails = false)
        {
            var dbSet = await GetDbSetAsync();

            return await dbSet
                .IncludeDetails(includeDetails)
                .Where(s => s.Name.StartsWith(name))
                .ToListAsync();
        }
    }
}
