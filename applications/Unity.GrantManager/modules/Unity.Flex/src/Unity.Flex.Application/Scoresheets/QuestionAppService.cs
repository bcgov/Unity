using System;
using System.Threading.Tasks;
using Unity.Flex.Domain.Scoresheets;
using Volo.Abp.Validation;

namespace Unity.Flex.Scoresheets
{
    public class QuestionAppService : FlexAppService, IQuestionAppService
    {
        private readonly IQuestionRepository _questionRepository;

        public QuestionAppService(IQuestionRepository questionRepository)
        {
            _questionRepository = questionRepository;
        }

        public virtual async Task<QuestionDto> GetAsync(Guid id)
        {
            return ObjectMapper.Map<Question, QuestionDto>(await _questionRepository.GetAsync(id));
        }

        public async Task<QuestionDto> UpdateAsync(Guid id, EditQuestionDto dto)
        {
            var question = await _questionRepository.GetAsync(dto.QuestionId) ?? throw new AbpValidationException("Missing QuestionId:" + dto.QuestionId);
            question.Name = dto.Name;
            question.Label = dto.Label;
            question.Description = dto.Description;
            return ObjectMapper.Map<Question, QuestionDto>(await _questionRepository.UpdateAsync(question));
        }

        public async Task DeleteAsync(Guid id)
        {
            await _questionRepository.DeleteAsync(id);
        }
    }
}
