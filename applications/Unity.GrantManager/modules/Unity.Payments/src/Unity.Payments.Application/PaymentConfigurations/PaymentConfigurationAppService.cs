using System.Threading.Tasks;
using Unity.Payments.Domain.Exceptions;
using Unity.Payments.Domain.PaymentConfigurations;
using Volo.Abp.Features;

namespace Unity.Payments.PaymentConfigurations
{
    [RequiresFeature("Unity.Payments")]
    public class PaymentConfigurationAppService(IPaymentConfigurationRepository paymentConfigurationRepository) : PaymentsAppService, IPaymentConfigurationAppService
    {


        public virtual async Task<PaymentConfigurationDto?> GetAsync()
        {
            PaymentConfiguration? paymentConfiguration = await FindPaymentConfigurationAsync();

            if (paymentConfiguration == null) { return null; }

            return ObjectMapper.Map<PaymentConfiguration, PaymentConfigurationDto>(paymentConfiguration);
        }

        public virtual async Task<PaymentConfigurationDto> CreateAsync(CreatePaymentConfigurationDto createPaymentConfigurationDto)
        {
            PaymentConfiguration? paymentConfiguration = await FindPaymentConfigurationAsync();

            if (paymentConfiguration != null)
            {
                throw new ConfigurationExistsException(L[ErrorConsts.ConfigurationExists]);
            }

            var newPaymentConfiguration = await paymentConfigurationRepository.InsertAsync(new PaymentConfiguration
            (
                createPaymentConfigurationDto.PaymentThreshold,
                createPaymentConfigurationDto.PaymentIdPrefix
            ));

            return ObjectMapper.Map<PaymentConfiguration, PaymentConfigurationDto>(newPaymentConfiguration);
        }

        public virtual async Task<PaymentConfigurationDto> UpdateAsync(UpdatePaymentConfigurationDto updatePaymentConfigurationDto)
        {
            PaymentConfiguration? paymentConfiguration = await FindPaymentConfigurationAsync() ??
                throw new ConfigurationExistsException(L[ErrorConsts.ConfigurationDoesNotExist]);

            paymentConfiguration.PaymentThreshold = updatePaymentConfigurationDto.PaymentThreshold;
            paymentConfiguration.PaymentIdPrefix = updatePaymentConfigurationDto.PaymentIdPrefix;

            var updatedConfiguration = await paymentConfigurationRepository.UpdateAsync(paymentConfiguration);

            return ObjectMapper.Map<PaymentConfiguration, PaymentConfigurationDto>(updatedConfiguration);
        }
        

        protected virtual async Task<PaymentConfiguration?> FindPaymentConfigurationAsync()
        {
            var paymentConfigurations = await paymentConfigurationRepository.GetListAsync();
            var paymentConfiguration = paymentConfigurations.Count > 0 ? paymentConfigurations[0] : null;
            return paymentConfiguration;
        }
    }
}
