using Microsoft.AspNetCore.Authorization;
using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.Flex.Domain.Scoresheets;
using Volo.Abp.Domain.Entities;

namespace Unity.Flex.Scoresheets
{
    [Authorize]
    public class SectionAppService(IScoresheetSectionRepository sectionRepository,
        IScoresheetRepository scoresheetRepository) : FlexAppService, ISectionAppService
    {
        public virtual async Task<ScoresheetSectionDto> GetAsync(Guid id)
        {
            return ObjectMapper.Map<ScoresheetSection, ScoresheetSectionDto>(await sectionRepository.GetAsync(id));
        }

        public async Task<ScoresheetSectionDto> UpdateAsync(Guid id, EditSectionDto dto)
        {
            (Scoresheet scoresheet, ScoresheetSection section) = await GetScoresheetAndSectionAsync(id);

            _ = scoresheet.UpdateSection(section, dto.Name.Trim());
            return ObjectMapper.Map<ScoresheetSection, ScoresheetSectionDto>(section);
        }

        public async Task DeleteAsync(Guid id)
        {
            await sectionRepository.DeleteAsync(id);
        }

        private async Task<(Scoresheet scoresheet, ScoresheetSection section)> GetScoresheetAndSectionAsync(Guid sectionId)
        {
            var scoresheet = await scoresheetRepository.GetBySectionAsync(sectionId, true) ?? throw new EntityNotFoundException();
            var section = scoresheet.Sections.FirstOrDefault(s => s.Id == sectionId) ?? throw new EntityNotFoundException();

            return (scoresheet, section);
        }
    }
}
