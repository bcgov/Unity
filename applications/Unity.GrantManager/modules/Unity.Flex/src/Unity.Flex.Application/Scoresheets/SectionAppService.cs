using System;
using System.Threading.Tasks;
using Unity.Flex.Domain.Scoresheets;
using Volo.Abp.Validation;

namespace Unity.Flex.Scoresheets
{
    public class SectionAppService : FlexAppService, ISectionAppService
    {
        private readonly IScoresheetSectionRepository _sectionRepository;

        public SectionAppService(IScoresheetSectionRepository sectionRepository)
        {
            _sectionRepository = sectionRepository;
        }

        public virtual async Task<ScoresheetSectionDto> GetAsync(Guid id)
        {
            return ObjectMapper.Map<ScoresheetSection, ScoresheetSectionDto>(await _sectionRepository.GetAsync(id));
        }

        public async Task<ScoresheetSectionDto> UpdateAsync(EditSectionDto dto)
        {
            var section = await _sectionRepository.GetAsync(dto.SectionId) ?? throw new AbpValidationException("Missing SectionId:" + dto.SectionId);
            section.Name = dto.Name;
            return ObjectMapper.Map<ScoresheetSection, ScoresheetSectionDto>(await _sectionRepository.UpdateAsync(section));
        }

        public async Task DeleteAsync(Guid id)
        {
            await _sectionRepository.DeleteAsync(id);
        }
    }
}
