using System.Threading.Tasks;

namespace Unity.Payments.PaymentConfigurations
{
    public interface IPaymentConfigurationAppService
    {
        Task<PaymentConfigurationDto?> GetAsync();       
        Task<PaymentConfigurationDto> UpdateAsync(UpdatePaymentConfigurationDto updatePaymentConfigurationDto);
        Task<PaymentConfigurationDto> CreateAsync(CreatePaymentConfigurationDto createUpdatePaymentConfigurationDto);
    }
}
