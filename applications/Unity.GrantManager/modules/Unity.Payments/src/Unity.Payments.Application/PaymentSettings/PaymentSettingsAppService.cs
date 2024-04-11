using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Features;
using Volo.Abp.MultiTenancy;

namespace Unity.Payments.PaymentSettings
{
    [RequiresFeature("Unity.Payments")]
    [Authorize]
    public class PaymentSettingsAppService : PaymentsAppService, IPaymentSettingsAppService
    {
        protected IPaymentSettingsRepository PaymentSettingsRepository { get; }

        public PaymentSettingsAppService(IPaymentSettingsRepository _PaymentSettingsRepository)
        {
            PaymentSettingsRepository = _PaymentSettingsRepository;
        }

        public PaymentSettingsDto Get()
        {
            var paymentSettingDto = new PaymentSettingsDto();

            if (CurrentTenant.IsAvailable)
            {
                IQueryable<PaymentSetting> queryablePaymentSettings = PaymentSettingsRepository.GetQueryableAsync().Result;
                var paymentSetting = queryablePaymentSettings.Where(x => x.TenantId == CurrentTenant.GetId()).First();
                paymentSettingDto = ObjectMapper.Map<PaymentSetting, PaymentSettingsDto>(paymentSetting);
            }

            return paymentSettingDto;
        }

        public async Task CreateOrUpdatePaymentSettingsAsync(PaymentSettingsDto paymentSettingsDto)
        {
            var paymentSetting = await PaymentSettingsRepository.FirstOrDefaultAsync(e => e.TenantId == CurrentTenant.GetId());
            if (paymentSetting == null && paymentSettingsDto != null)
            {
                await PaymentSettingsRepository.InsertAsync(new PaymentSetting
                {
                    PaymentThreshold = paymentSettingsDto.PaymentThreshold,
                    MinistryClient = paymentSettingsDto.MinistryClient,
                    Responsibility = paymentSettingsDto.Responsibility,
                    Stob = paymentSettingsDto.Stob,
                    ServiceLine = paymentSettingsDto.ServiceLine,
                    ProjectNumber = paymentSettingsDto.ProjectNumber
                }, autoSave: true);
            }
            else if (paymentSetting != null && paymentSettingsDto != null)
            {
                paymentSetting.PaymentThreshold = paymentSettingsDto.PaymentThreshold;
                paymentSetting.MinistryClient = paymentSettingsDto.MinistryClient;
                paymentSetting.Responsibility = paymentSettingsDto.Responsibility;
                paymentSetting.Stob = paymentSettingsDto.Stob;
                paymentSetting.ServiceLine = paymentSettingsDto.ServiceLine;
                paymentSetting.ProjectNumber = paymentSettingsDto.ProjectNumber;
                await PaymentSettingsRepository.UpdateAsync(paymentSetting, autoSave: true);
            }
        }

    } 
}
