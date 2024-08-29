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

        public async Task<ScoresheetSection?> GetSectionWithHighestOrderAsync(Guid scoresheetId)
        {
            var dbContext = await GetDbContextAsync();

            var highestOrderSection = await dbContext.ScoresheetSections
                .Where(sec => sec.ScoresheetId == scoresheetId)
                .OrderByDescending(sec => sec.Order)
                .FirstOrDefaultAsync();

            return highestOrderSection;

        }

        public async Task<bool> HasSectionWithNameAsync(Guid scoresheetId, string sectionName)
        {
            var dbContext = await GetDbContextAsync();

            var sections = await dbContext.ScoresheetSections
                .Where(sec => sec.ScoresheetId == scoresheetId)
                .ToListAsync();

            return sections.Exists(s => s.Name == sectionName);
        }

        public async Task<bool> HasQuestionWithNameAsync(Guid scoresheetId, string questionName)
        {
            var dbContext = await GetDbContextAsync();

            return await dbContext.ScoresheetSections
                .Where(sec => sec.ScoresheetId == scoresheetId)
                .AnyAsync(sec => sec.Fields.Any(q => q.Name == questionName));
        }
    }
}
