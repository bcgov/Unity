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
        public async Task<Worksheet?> GetByCorrelationAnchorAsync(Guid correlationId, string correlationProvider, string uiAnchor, bool includeDetails = false)
        {
            var dbSet = await GetDbSetAsync();

            return await dbSet
                    .IncludeDetails(includeDetails)
                    .FirstOrDefaultAsync(s => s.Links.Any(s => s.CorrelationId == correlationId && s.CorrelationProvider == correlationProvider && s.UiAnchor == uiAnchor));
        }

        public async Task<Worksheet?> GetByCorrelationByNameAsync(Guid correlationId, string correlationProvider, string name, bool includeDetails = false)
        {
            var dbSet = await GetDbSetAsync();

            return await dbSet
                    .IncludeDetails(includeDetails)
                    .FirstOrDefaultAsync(s => s.Links.Any(s => s.CorrelationId == correlationId && s.CorrelationProvider == correlationProvider)
                    && s.Name == name);
        }

        public async Task<Worksheet?> GetByNameAsync(string name, bool includeDetails = false)
        {
            var dbSet = await GetDbSetAsync();

            return await dbSet
                .IncludeDetails(includeDetails)
                .FirstOrDefaultAsync(s => s.Name == name);
        }

        public async Task<Worksheet?> GetBySectionAsync(Guid id, bool includeDetails = false)
        {
            var dbSet = await GetDbSetAsync();

            return await dbSet
                .IncludeDetails(includeDetails)
                .FirstOrDefaultAsync(s => s.Sections.Any(s => s.Id == id));
        }

        public async Task<List<Worksheet>> GetListOrderedAsync(Guid correlationId, string correlationProvider, bool includeDetails = false)
        {
            var dbSet = await GetDbSetAsync();

            return await dbSet
                .IncludeDetails(includeDetails)
                .Where(s => s.Links.Any(s => s.CorrelationId == correlationId && s.CorrelationProvider == correlationProvider))
                .OrderBy(s => s.Name).ToListAsync();
        }

        public async Task<List<Worksheet>> GetListAsync(bool includeDetails = false)
        {
            var dbSet = await GetDbSetAsync();

            return await dbSet
                .IncludeDetails(includeDetails)
                .ToListAsync();
        }

        public async Task<Worksheet> GetAsync(Guid id, bool includeDetails = true)
        {
            var dbSet = await GetDbSetAsync();

            return await dbSet
                .IncludeDetails(includeDetails)
                .FirstAsync(s => s.Id == id);
        }

        public async Task<List<Worksheet>> GetByNameStartsWithAsync(string name, bool includeDetails = false)
        {
            var dbSet = await GetDbSetAsync();

            return await dbSet
                .IncludeDetails(includeDetails)
                .Where(s => s.Name.StartsWith(name))
                .ToListAsync();
        }
    }
}
