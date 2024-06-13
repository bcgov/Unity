using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.Flex.Domain.ScoresheetInstances;
using Unity.Flex.Domain.Scoresheets;
using Volo.Abp.Validation;

namespace Unity.Flex.Scoresheets
{
    public class ScoresheetInstanceAppService : FlexAppService, IScoresheetInstanceAppService
    {
        private readonly IScoresheetInstanceRepository _instanceRepository;

        public ScoresheetInstanceAppService(IScoresheetInstanceRepository instanceRepository)
        {
            _instanceRepository = instanceRepository;
        }

        public async Task SaveAnswer(Guid assessmentId, Guid questionId, double answer)
        {
            var instance = await _instanceRepository.GetByCorrelationAsync(assessmentId) ?? throw new AbpValidationException("Missing ScoresheetInstance.");
            var ans = instance.Answers.FirstOrDefault(a => a.QuestionId == questionId);
            if (ans != null)
            {
                ans.CurrentScore = answer;
            }
            else
            {
                ans = new Answer(Guid.NewGuid()) { CurrentScore = answer, QuestionId = questionId, ScoresheetInstanceId = instance.Id };
                instance.Answers.Add(ans);
            }

            await _instanceRepository.UpdateAsync(instance);
        }
    }
}
