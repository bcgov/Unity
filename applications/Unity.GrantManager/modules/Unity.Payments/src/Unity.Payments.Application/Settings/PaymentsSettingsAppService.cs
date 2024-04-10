using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using Unity.Payments.BatchPaymentRequests;
using Volo.Abp.Features;
using Volo.Abp.MultiTenancy;
using Volo.Abp.SettingManagement;

namespace Unity.Payments.Settings
{
    [RequiresFeature("Unity.Payments")]
    [Authorize]
    public class PaymentsSettingsAppService : PaymentsAppService, IPaymentsSettingsAppService
    {
        protected ISettingManager SettingManager { get; }

        public PaymentsSettingsAppService(ISettingManager settingManager)
        {
            SettingManager = settingManager;
        }

        public virtual async Task<PaymentsSettingsDto> GetAsync()
        {
            var settingsDto = new PaymentsSettingsDto
            {                
                PaymentThreshold = decimal.Parse(await SettingProvider.GetOrNullAsync(PaymentsSettings.PaymentThreshold) ?? PaymentConsts.DefaultThresholdAmount.ToString("0.00"))
            };

            if (CurrentTenant.IsAvailable)
            {
                settingsDto.PaymentThreshold = decimal.Parse(await SettingManager.GetOrNullForTenantAsync(PaymentsSettings.PaymentThreshold, CurrentTenant.GetId(), true));
            }

            return settingsDto;
        }

        public virtual async Task UpdateAsync(UpdatePaymentsSettingsDto input)
        {
            await SettingManager.SetForTenantOrGlobalAsync(CurrentTenant.Id, PaymentsSettings.PaymentThreshold, input.PaymentThreshold.ToString("0.00"));
        }
    } 
}
