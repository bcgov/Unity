using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System;
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
            await PublishCustomFieldUpdatesAsync(id, FlexConsts.PaymentInfoUiAnchor, input);

            return new PaymentInfoDto();
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
    }
}