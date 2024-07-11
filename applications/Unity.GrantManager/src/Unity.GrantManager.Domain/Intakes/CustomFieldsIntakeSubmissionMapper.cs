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
using Unity.Flex.Worksheets;
using Microsoft.Extensions.Logging;

namespace Unity.GrantManager.Intakes
{
    [IntegrationService]
    public class CustomFieldsIntakeSubmissionMapper(IFeatureChecker featureChecker,
        ILocalEventBus localEventBus) : DomainService
    {
        public async Task MapAndPersistCustomFields(Guid applicationId,
            Guid formVersionId,
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
                    try
                    {
                        foreach (JProperty property in configMap.Properties())
                        {
                            var dataKey = property.Name;
                            if (dataKey.StartsWith("custom_"))
                            {
                                var field = TrimTypeFromFieldName(dataKey);
                                var token = data.SelectToken(property.Value.ToString());
                                var fieldType = ResolveFieldType(dataKey);
                                var value = ((JToken)token).ApplyTransformer(fieldType);                                
                                customIntakeValues.Add(new(field, value.ApplySerializer()));
                            }
                        }
                    }
                    catch (InvalidCastException ex)
                    {
                        Logger.LogException(ex);
                    }
                }

                await localEventBus.PublishAsync(new CreateWorksheetInstanceByFieldValuesEto()
                {
                    SheetCorrelationId = formVersionId,
                    SheetCorrelationProvider = CorrelationConsts.FormVersion,
                    InstanceCorrelationId = applicationId,
                    InstanceCorrelationProvider = CorrelationConsts.Application,
                    CustomFields = customIntakeValues
                });
            }
        }

        private static string TrimTypeFromFieldName(string fieldName)
        {
            return fieldName[..fieldName.IndexOf('.')];
        }

        private static CustomFieldType ResolveFieldType(string fieldName)
        {
            // field formst "custom_worksheet_name.type"
            var type = fieldName[(fieldName.IndexOf('.') + 1)..];
            var parsed = Enum.TryParse(type, out CustomFieldType enumType);
            if (parsed) return enumType;
            return CustomFieldType.Text;
        }
    }
}
