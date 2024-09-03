using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.Flex.Domain.Scoresheets;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace Unity.Flex.EntityFrameworkCore.Repositories
{
    public class ScoresheetSectionRepository : EfCoreRepository<FlexDbContext, ScoresheetSection, Guid>, IScoresheetSectionRepository
    {      
        public ScoresheetSectionRepository(IDbContextProvider<FlexDbContext> dbContextProvider) : base(dbContextProvider)
        {
        }

        public async Task<ScoresheetSection?> GetSectionWithHighestOrderAsync(Guid scoresheetId, bool includeDetails = false)
        {
            var dbSet = await GetDbSetAsync();

            return await dbSet
                .IncludeDetails(includeDetails)
                .Where(sec => sec.ScoresheetId == scoresheetId)
                .OrderByDescending(sec => sec.Order)
                .FirstOrDefaultAsync();
        }        

    }
}
