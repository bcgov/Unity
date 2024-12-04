using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.Flex.Domain.ScoresheetInstances;
using Unity.Flex.Domain.Scoresheets;
using Unity.Flex.Scoresheets;
using Unity.Flex.Scoresheets.Events;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;
using Volo.Abp.Validation;

namespace Unity.Flex.Handlers
{
    public class PersistScoresheetSectionInstanceHandler(IScoresheetInstanceRepository scoresheetInstanceRepository) : ILocalEventHandler<PersistScoresheetSectionInstanceEto>, ITransientDependency
    {
        public async Task HandleEventAsync(PersistScoresheetSectionInstanceEto eventData)
        {

            var instance = await scoresheetInstanceRepository.GetByCorrelationAsync(eventData.AssessmentId) ?? throw new AbpValidationException("Missing ScoresheetInstance.");
            var scoresheetAnswers = eventData.AssessmentAnswers.ToList();

            foreach (var item in scoresheetAnswers)
            {
                var ans = instance.Answers.FirstOrDefault(a => a.QuestionId == item.QuestionId);

                if (ans != null)
                {
                    ans.SetValue(ValueConverter.Convert(item.Answer ?? "", (QuestionType)item.QuestionType));
                }
                else
                {
                    ans = new Answer(Guid.NewGuid())
                    {
                        CurrentValue = ValueConverter.Convert(item?.Answer?.ToString() ?? string.Empty, (QuestionType)item!.QuestionType),
                        QuestionId = item.QuestionId,
                        ScoresheetInstanceId = instance.Id
                    };
                    instance.Answers.Add(ans);
                }

                await scoresheetInstanceRepository.UpdateAsync(instance);
            }

        }
    }
}
