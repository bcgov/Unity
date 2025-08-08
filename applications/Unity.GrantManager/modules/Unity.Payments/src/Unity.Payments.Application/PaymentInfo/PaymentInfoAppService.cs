using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Unity.Flex.WorksheetInstances;
using Unity.Flex.Worksheets;
using Unity.GrantManager.Flex;
using Unity.Modules.Shared.Correlation;
using Volo.Abp.EventBus.Local;
using Volo.Abp.Features;

namespace Unity.Payments.PaymentInfo
{
    [RequiresFeature("Unity.Payments")]
    [Authorize]
    public class PaymentInfoAppService(ILocalEventBus localEventBus) : PaymentsAppService, IPaymentInfoAppService
    {
        public async Task<PaymentInfoDto> UpdateAsync(Guid id, CreateUpdatePaymentInfoDto input)
        {
            // Handle custom fields for payment info
            if (HasValue(input.CustomFields) && input.CorrelationId != Guid.Empty)
            {
                // Handle multiple worksheets
                if (input.WorksheetIds?.Count > 0)
                {
                    foreach (var worksheetId in input.WorksheetIds)
                    {
                        var worksheetCustomFields = ExtractCustomFieldsForWorksheet(input.CustomFields, worksheetId);
                        if (worksheetCustomFields.Count > 0)
                        {
                            var worksheetData = new CustomDataFieldDto
                            {
                                WorksheetId = worksheetId,
                                CustomFields = worksheetCustomFields,
                                CorrelationId = input.CorrelationId
                            };
                            await PublishCustomFieldUpdatesAsync(id, FlexConsts.PaymentInfoUiAnchor, worksheetData);
                        }
                    }
                }
                // Fallback for single worksheet (backward compatibility)
                else if (input.WorksheetId != Guid.Empty)
                {
                    await PublishCustomFieldUpdatesAsync(id, FlexConsts.PaymentInfoUiAnchor, input);
                }
            }

            return new PaymentInfoDto();
        }

        private static bool HasValue(JsonElement element)
        {
            return element.ValueKind != JsonValueKind.Null && element.ValueKind != JsonValueKind.Undefined;
        }

        protected virtual async Task PublishCustomFieldUpdatesAsync(Guid applicationId,
            string uiAnchor,
            CustomDataFieldDto input)
        {
            if (await FeatureChecker.IsEnabledAsync("Unity.Flex"))
            {
                if (input.CorrelationId != Guid.Empty)
                {
                    await localEventBus.PublishAsync(new PersistWorksheetIntanceValuesEto()
                    {
                        InstanceCorrelationId = applicationId,
                        InstanceCorrelationProvider = CorrelationConsts.Application,
                        SheetCorrelationId = input.CorrelationId,
                        SheetCorrelationProvider = CorrelationConsts.FormVersion,
                        UiAnchor = uiAnchor,
                        CustomFields = input.CustomFields,
                        WorksheetId = input.WorksheetId
                    });
                }
                else
                {
                    Logger.LogError("Unable to resolve for version");
                }
            }
        }        

        private static Dictionary<string, object> ExtractCustomFieldsForWorksheet(dynamic customFields, Guid worksheetId)
        {
            var result = new Dictionary<string, object>();
            var worksheetSuffix = $".{worksheetId}";

            if (customFields is JsonElement jsonElement)
            {
                result = jsonElement.EnumerateObject()
                    .Where(property => property.Name.EndsWith(worksheetSuffix))
                    .ToDictionary(
                        property => property.Name[..^worksheetSuffix.Length],
                        property => property.Value.ValueKind == JsonValueKind.String ? (object)property.Value.GetString()! : string.Empty
                    );
            }

            return result;
        }
    }
}