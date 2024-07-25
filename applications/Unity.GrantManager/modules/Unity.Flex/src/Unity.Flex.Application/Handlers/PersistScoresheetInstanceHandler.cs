using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.Flex.Domain.ScoresheetInstances;
using Unity.Flex.Domain.Scoresheets;
using Unity.Flex.Scoresheets;
using Unity.Flex.Scoresheets.Events;
using Unity.Flex.Worksheets.Values;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;
using Volo.Abp.Validation;

namespace Unity.Flex.Handlers
{
    public class PersistScoresheetInstanceHandler(IScoresheetInstanceRepository scoresheetInstanceRepository) : ILocalEventHandler<PersistScoresheetInstanceEto>, ITransientDependency
    {
        // swap over to app service use instead of repository directly
        public async Task HandleEventAsync(PersistScoresheetInstanceEto eventData)
        {

            var instance = await scoresheetInstanceRepository.GetByCorrelationAsync(eventData.CorrelationId) ?? throw new AbpValidationException("Missing ScoresheetInstance.");
            var ans = instance.Answers.FirstOrDefault(a => a.QuestionId == eventData.QuestionId);

            if (ans != null)
            {
                ans.SetValue(ValueConverter.Convert(eventData.Answer ?? "", (QuestionType)eventData.QuestionType));
            }
            else
            {
                if (eventData != null)
                {
                    ans = new Answer(Guid.NewGuid())
                    {
                        CurrentValue = ValueConverter.Convert(eventData?.Answer?.ToString() ?? string.Empty, (QuestionType)eventData!.QuestionType),
                        QuestionId = eventData.QuestionId,
                        ScoresheetInstanceId = instance.Id
                    };
                    instance.Answers.Add(ans);
                }
            }

            await scoresheetInstanceRepository.UpdateAsync(instance);
        }
    }
}
