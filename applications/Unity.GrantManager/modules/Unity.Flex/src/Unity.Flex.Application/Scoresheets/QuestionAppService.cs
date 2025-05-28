using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;
using Unity.Flex.Domain.Scoresheets;
using Unity.Flex.Scoresheets.Enums;
using Unity.Flex.Worksheets.Definitions;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Validation;

namespace Unity.Flex.Scoresheets
{
    [Authorize]
    public class QuestionAppService(IQuestionRepository questionRepository) : FlexAppService, IQuestionAppService
    {
        public virtual async Task<QuestionDto> GetAsync(Guid id)
        {
            var question = await questionRepository.GetAsync(id) ?? throw new EntityNotFoundException();

            return ObjectMapper.Map<Question, QuestionDto>(question);
        }

        public async Task<QuestionDto> UpdateAsync(Guid id, EditQuestionDto dto)
        {
            var questionName = dto.Name.Trim();
            var question = await questionRepository.GetAsync(id) ?? throw new AbpValidationException("Missing QuestionId:" + id);
            question.SetName(questionName);
            question.Label = dto.Label;
            question.Description = dto.Description;
            question.Type = (QuestionType)dto.QuestionType;
            question.Definition = DefinitionResolver.Resolve(question.Type, dto.Definition);
            return ObjectMapper.Map<Question, QuestionDto>(await questionRepository.UpdateAsync(question));
        }

        public async Task DeleteAsync(Guid id)
        {
            await questionRepository.DeleteAsync(id);
        }
    }
}
