using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;
using Unity.Flex.Domain.Scoresheets;
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
            var questionDto = ObjectMapper.Map<Question, QuestionDto>(question);
            if(question.Answers.Count > 0)
            {
                questionDto.HasAnswers = true;
            }
            return questionDto;
        }

        public async Task<QuestionDto> UpdateAsync(Guid id, EditQuestionDto dto)
        {
            var question = await _questionRepository.GetAsync(id) ?? throw new AbpValidationException("Missing QuestionId:" + id);
            question.Name = dto.Name;
            question.Label = dto.Label;
            question.Description = dto.Description;
            question.Type = (QuestionType)dto.QuestionType;
            return ObjectMapper.Map<Question, QuestionDto>(await _questionRepository.UpdateAsync(question));
        }

        public async Task DeleteAsync(Guid id)
        {
            await _questionRepository.DeleteAsync(id);
        }
    }
}
