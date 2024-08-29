using System;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace Unity.Flex.Domain.Scoresheets
{
    public interface IScoresheetSectionRepository : IBasicRepository<ScoresheetSection, Guid>
    {
        public Task<ScoresheetSection?> GetSectionWithHighestOrderAsync(Guid scoresheetId);
        public Task<bool> HasSectionWithNameAsync(Guid scoresheetId, string sectionName);
        public Task<bool> HasQuestionWithNameAsync(Guid scoresheetId, string questionName);
    }
}
