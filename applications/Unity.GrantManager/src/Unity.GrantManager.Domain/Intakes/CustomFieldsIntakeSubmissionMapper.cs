using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Volo.Abp.Features;
using System;
using System.Collections.Generic;
using Volo.Abp.EventBus.Local;
using Volo.Abp;
using Volo.Abp.Domain.Services;
using Unity.Flex.WorksheetInstances;
using Unity.Modules.Shared.Correlation;

namespace Unity.GrantManager.Intakes
{
    [IntegrationService]
    public class CustomFieldsIntakeSubmissionMapper(IFeatureChecker featureChecker,
        ILocalEventBus localEventBus) : DomainService
    {
        public async Task MapAndPersistCustomFields(Guid applicationId,
            dynamic formSubmission,
            string? formVersionSubmissionHeaderMapping)
        {
            if (await featureChecker.IsEnabledAsync("Unity.Flex"))
            {
                if (formVersionSubmissionHeaderMapping == null) return;

                List<KeyValuePair<string, object?>> customIntakeValues = [];

                var submission = formSubmission.submission;
                var data = submission.submission.data;

                var configMap = JsonConvert.DeserializeObject<dynamic>(formVersionSubmissionHeaderMapping)!;

                if (configMap != null)
                {
                    foreach (JProperty property in configMap.Properties())
                    {
                        var dataKey = property.Name;
                        if (dataKey.StartsWith("custom_"))
                        {
                            customIntakeValues.Add(new(dataKey, data.SelectToken(property.Value.ToString())));
                        }
                    }
                }

                await localEventBus.PublishAsync(new CreateWorksheetInstanceByFieldValuesEto()
                {
                    CorrelationId = applicationId,
                    CorrelationProvider = CorrelationConsts.Application,
                    CustomFields = customIntakeValues
                });
            }
        }
    }
}
