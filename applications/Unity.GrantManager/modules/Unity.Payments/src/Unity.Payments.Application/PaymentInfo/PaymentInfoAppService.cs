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
            if (HasValue(input.CustomFields) && input.CorrelationId != Guid.Empty)
            {
                if (input.WorksheetIds?.Count > 0)
                {
                    foreach (var worksheetId in input.WorksheetIds)
                    {
                        var worksheetCustomFields = ExtractCustomFieldsForWorksheet(input.CustomFields, worksheetId);
                        if (worksheetCustomFields.Count > 0)
                        {
                            await PublishCustomFieldUpdatesAsync(id, FlexConsts.PaymentInfoUiAnchor, new CustomDataFieldDto
                            {
                                WorksheetId = worksheetId,
                                CustomFields = worksheetCustomFields,
                                CorrelationId = input.CorrelationId
                            });
                        }
                    }
                }
                else if (input.WorksheetId != Guid.Empty)
                {
                    await PublishCustomFieldUpdatesAsync(id, FlexConsts.PaymentInfoUiAnchor, input);
                }
            }

            return new PaymentInfoDto();
        }

        private static bool HasValue(JsonElement element) =>
            element.ValueKind switch
            {
                JsonValueKind.Object => element.EnumerateObject().Any(),
                JsonValueKind.Array => element.EnumerateArray().Any(),
                JsonValueKind.String => !string.IsNullOrWhiteSpace(element.GetString()),
                JsonValueKind.Number => true,
                JsonValueKind.True => true,
                JsonValueKind.False => true,
                _ => false
            };

        protected virtual async Task PublishCustomFieldUpdatesAsync(Guid applicationId, string uiAnchor, CustomDataFieldDto input)
        {
            if (!await FeatureChecker.IsEnabledAsync("Unity.Flex"))
                return;

            if (input.CorrelationId == Guid.Empty)
            {
                Logger.LogError("Unable to resolve worksheet version: CorrelationId is empty.");
                return;
            }

            await localEventBus.PublishAsync(new PersistWorksheetIntanceValuesEto
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

        private static Dictionary<string, object> ExtractCustomFieldsForWorksheet(JsonElement customFields, Guid worksheetId)
        {
            var worksheetSuffix = $".{worksheetId}";

            return customFields.EnumerateObject()
                .Where(p => p.Name.EndsWith(worksheetSuffix))
                .ToDictionary(
                    p => p.Name[..^worksheetSuffix.Length],
                    p => p.Value.ValueKind switch
                    {
                        JsonValueKind.String => (object)p.Value.GetString()!,
                        JsonValueKind.Number => p.Value.GetDouble(),
                        JsonValueKind.True => true,
                        JsonValueKind.False => false,
                        _ => string.Empty
                    }
                );
        }
    }
}
