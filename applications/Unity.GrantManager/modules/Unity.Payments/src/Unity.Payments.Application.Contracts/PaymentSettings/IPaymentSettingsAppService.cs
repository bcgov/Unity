using System.Threading.Tasks;

namespace Unity.Payments.PaymentSettings
{
    public interface IPaymentSettingsAppService
    {
        PaymentSettingsDto Get();

        Task CreateOrUpdatePaymentSettingsAsync(PaymentSettingsDto paymentSettingsDto);
    }
}
