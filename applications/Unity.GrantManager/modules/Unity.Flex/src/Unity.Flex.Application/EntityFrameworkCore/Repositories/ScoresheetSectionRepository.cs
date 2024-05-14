using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Linq.Dynamic.Core;
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

        public async Task<ScoresheetSection?> GetSectionWithHighestOrderAsync(Guid scoresheetId)
        {
            var dbContext = await GetDbContextAsync();

            var highestOrderSection = await dbContext.ScoresheetSections
                .Where(sec => sec.ScoresheetId == scoresheetId)
                .OrderByDescending(sec => sec.Order)
                .FirstOrDefaultAsync();

            return highestOrderSection;

        }

    }
}
