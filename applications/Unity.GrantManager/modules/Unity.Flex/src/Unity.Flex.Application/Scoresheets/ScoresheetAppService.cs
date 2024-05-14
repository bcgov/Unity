using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Flex.Domain.Scoresheets;
using Unity.Flex.Worksheets;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.ObjectMapping;
using Volo.Abp.Validation;
using static System.Collections.Specialized.BitVector32;

namespace Unity.Flex.Scoresheets
{
    public class ScoresheetAppService : FlexAppService, IScoresheetAppService
    {
        private readonly IScoresheetRepository _scoresheetRepository;
        private readonly IScoresheetSectionRepository _sectionRepository;
        private readonly IQuestionRepository _questionRepository;

        private readonly static object _lockObject = new();
        public ScoresheetAppService(IScoresheetRepository scoresheetRepository, IScoresheetSectionRepository sectionRepository, IQuestionRepository questionRepository) 
        {
            _scoresheetRepository = scoresheetRepository;
            _sectionRepository = sectionRepository;
            _questionRepository = questionRepository;
        }
        public async Task<ScoresheetDto> CreateAsync(CreateScoresheetDto dto)
        {
            var result = await _scoresheetRepository.InsertAsync(new Scoresheet (Guid.NewGuid(),dto.Name));
            return ObjectMapper.Map<Scoresheet, ScoresheetDto>(result);
        }

        public virtual Task<QuestionDto> CreateQuestionAsync(CreateQuestionDto dto)
        {
            throw new NotImplementedException();
        }

        public virtual Task<ScoresheetSectionDto> CreateSectionAsync(CreateSectionDto dto)
        {
            lock (_lockObject)
            {
                ScoresheetSection? highestOrderSection = _sectionRepository.GetSectionWithHighestOrderAsync(dto.ScoresheetId).Result;
                var order = highestOrderSection == null ? 0 : highestOrderSection.Order + 1;
                var result = _sectionRepository.InsertAsync(new ScoresheetSection(Guid.NewGuid(), dto.Name, order, dto.ScoresheetId)).Result;
                return Task.FromResult(ObjectMapper.Map<ScoresheetSection, ScoresheetSectionDto>(result));
            }
        }

        public async Task<List<ScoresheetDto>> GetAllAsync()
        {
            var result = await _scoresheetRepository.GetListWithChildrenAsync();
            return ObjectMapper.Map<List<Scoresheet>, List<ScoresheetDto>>(result);
        }

        public virtual async Task<ScoresheetDto> GetAsync(Guid scoresheetId)
        {
            return ObjectMapper.Map<Scoresheet, ScoresheetDto>(await _scoresheetRepository.GetAsync(scoresheetId));
        }

        public virtual async Task<QuestionDto> GetQuestionAsync(Guid questionId)
        {
            return ObjectMapper.Map<Question, QuestionDto>(await _questionRepository.GetAsync(questionId));
        }

        public virtual async Task<ScoresheetSectionDto> GetSectionAsync(Guid sectionId)
        {
            return ObjectMapper.Map<ScoresheetSection, ScoresheetSectionDto>(await _sectionRepository.GetAsync(sectionId));
        }

        public async Task<ScoresheetSectionDto> EditSectionAsync(EditSectionDto dto)
        {
            var section = await _sectionRepository.GetAsync(dto.SectionId) ?? throw new AbpValidationException("Missing SectionId:"+dto.SectionId);
            section.Name = dto.Name;
            return ObjectMapper.Map<ScoresheetSection, ScoresheetSectionDto>(await _sectionRepository.UpdateAsync(section));
        }

        public async Task<ScoresheetDto> EditAsync(EditScoresheetDto dto)
        {
            var scoresheet = await _scoresheetRepository.GetAsync(dto.ScoresheetId) ?? throw new AbpValidationException("Missing ScoresheetId:" + dto.ScoresheetId);
            scoresheet.Name = dto.Name;
            return ObjectMapper.Map<Scoresheet, ScoresheetDto>(await _scoresheetRepository.UpdateAsync(scoresheet));
        }
    }
}
