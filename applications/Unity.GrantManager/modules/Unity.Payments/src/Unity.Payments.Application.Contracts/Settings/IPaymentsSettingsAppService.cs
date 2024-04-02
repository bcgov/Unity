using System.Threading.Tasks;

namespace Unity.Payments.Settings
{
    public interface IPaymentsSettingsAppService
    {
        Task<PaymentsSettingsDto> GetAsync();
        Task UpdateAsync(UpdatePaymentsSettingsDto input);
    }
}
