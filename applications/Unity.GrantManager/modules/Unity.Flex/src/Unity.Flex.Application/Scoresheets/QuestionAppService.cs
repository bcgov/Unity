using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;
using Unity.Flex.Domain.Scoresheets;
using Unity.Flex.Worksheets.Definitions;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Validation;

namespace Unity.Flex.Scoresheets
{
    [Authorize]
    public class QuestionAppService : FlexAppService, IQuestionAppService
    {
        private readonly IQuestionRepository _questionRepository;
        
        public QuestionAppService(IQuestionRepository questionRepository)
        {
            _questionRepository = questionRepository;
        }

        public virtual async Task<QuestionDto> GetAsync(Guid id)
        {
            var question = await _questionRepository.GetAsync(id) ?? throw new EntityNotFoundException();

            return ObjectMapper.Map<Question, QuestionDto>(question);
            
        }

        public async Task<QuestionDto> UpdateAsync(Guid id, EditQuestionDto dto)
        {
            var questionName = dto.Name.Trim();
            var question = await _questionRepository.GetAsync(id) ?? throw new AbpValidationException("Missing QuestionId:" + id);
            question.SetName(questionName);
            question.Label = dto.Label;
            question.Description = dto.Description;
            question.Type = (QuestionType)dto.QuestionType;
            question.Definition = DefinitionResolver.Resolve(question.Type, dto.Definition);
            return ObjectMapper.Map<Question, QuestionDto>(await _questionRepository.UpdateAsync(question));
        }

        public async Task DeleteAsync(Guid id)
        {
            await _questionRepository.DeleteAsync(id);
        }
    }
}
