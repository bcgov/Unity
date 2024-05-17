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

        private readonly static object _sectionLockObject = new();
        private readonly static object _questionLockObject = new();
        public ScoresheetAppService(IScoresheetRepository scoresheetRepository, IScoresheetSectionRepository sectionRepository, IQuestionRepository questionRepository)
        {
            _scoresheetRepository = scoresheetRepository;
            _sectionRepository = sectionRepository;
            _questionRepository = questionRepository;
        }
        public async Task<ScoresheetDto> CreateAsync(CreateScoresheetDto dto)
        {
            var result = await _scoresheetRepository.InsertAsync(new Scoresheet(Guid.NewGuid(), dto.Name));
            return ObjectMapper.Map<Scoresheet, ScoresheetDto>(result);
        }

        public virtual Task<QuestionDto> CreateQuestionAsync(CreateQuestionDto dto)
        {
            lock (_questionLockObject)
            {
                ScoresheetSection highestOrderSection = _sectionRepository.GetSectionWithHighestOrderAsync(dto.ScoresheetId).Result ?? throw new AbpValidationException("Scoresheet has no section.");
                Question? highestOrderQuestion = _questionRepository.GetQuestionWithHighestOrderAsync(highestOrderSection.Id).Result;
                var order = highestOrderQuestion == null ? 0 : highestOrderQuestion.Order + 1;
                var result = _questionRepository.InsertAsync(new Question(Guid.NewGuid(), dto.Name, dto.Label, Domain.Enums.QuestionType.Number, order, dto.Description, highestOrderSection.Id)).Result;
                return Task.FromResult(ObjectMapper.Map<Question, QuestionDto>(result));
            }
        }

        public virtual Task<ScoresheetSectionDto> CreateSectionAsync(CreateSectionDto dto)
        {
            lock (_sectionLockObject)
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
            var section = await _sectionRepository.GetAsync(dto.SectionId) ?? throw new AbpValidationException("Missing SectionId:" + dto.SectionId);
            section.Name = dto.Name;
            return ObjectMapper.Map<ScoresheetSection, ScoresheetSectionDto>(await _sectionRepository.UpdateAsync(section));
        }

        public async Task<ScoresheetDto> EditAsync(EditScoresheetDto dto)
        {
            var scoresheet = await _scoresheetRepository.GetAsync(dto.ScoresheetId) ?? throw new AbpValidationException("Missing ScoresheetId:" + dto.ScoresheetId);
            scoresheet.Name = dto.Name;
            return ObjectMapper.Map<Scoresheet, ScoresheetDto>(await _scoresheetRepository.UpdateAsync(scoresheet));
        }

        public async Task<QuestionDto> EditQuestionAsync(EditQuestionDto dto)
        {
            var question = await _questionRepository.GetAsync(dto.QuestionId) ?? throw new AbpValidationException("Missing QuestionId:" + dto.QuestionId);
            question.Name = dto.Name;
            question.Label = dto.Label;
            question.Description = dto.Description;
            return ObjectMapper.Map<Question, QuestionDto>(await _questionRepository.UpdateAsync(question));
        }

        public async Task SaveOrder(List<ScoresheetItemDto> dto)
        {
            uint sectionOrder = 0;
            uint questionOrder = 0;
            ScoresheetSection? currentSection = null;
            foreach (var item in dto)
            {
                if (item.Type == "section")
                {
                    var section = await _sectionRepository.GetAsync(item.Id) ?? throw new AbpValidationException("SectionId not found.");
                    section.Order = sectionOrder;
                    sectionOrder++;
                    questionOrder = 0;
                    await _sectionRepository.UpdateAsync(section);
                    currentSection = section;
                }
                else if (item.Type == "question")
                {
                    var question = await _questionRepository.GetAsync(item.Id) ?? throw new AbpValidationException("QuestionId not found.");
                    question.Order = questionOrder;
                    questionOrder++;
                    if (currentSection != null)
                    {
                        question.SectionId = currentSection.Id;
                    }
                    await _questionRepository.UpdateAsync(question);
                }
                else
                {
                    throw new AbpValidationException("Invalid type!");
                }
            }
                
        }

        public async Task DeleteAsync(Guid scoresheetId)
        {
            await _scoresheetRepository.DeleteAsync(scoresheetId);
        }

        public async Task DeleteSectionAsync(Guid sectionId)
        {
            await _sectionRepository.DeleteAsync(sectionId);
        }

        public async Task DeleteQuestionAsync(Guid questionId)
        {
            await _questionRepository.DeleteAsync(questionId);
        }
    }
}
