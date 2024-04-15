using System.Threading.Tasks;

namespace Unity.Payments.PaymentConfigurations
{
    public interface IPaymentConfigurationAppService
    {
        Task<PaymentConfigurationDto?> GetAsync();

        Task<PaymentConfigurationDto> CreateAsync(CreatePaymentConfigurationDto createPaymentConfigurationDto);

        Task<PaymentConfigurationDto> UpdateAsync(UpdatePaymentConfigurationDto updatePaymentConfigurationDto);
        Task<string?> GetAccountDistributionCodeAsync();
	}
}
