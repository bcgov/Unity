using System.Threading.Tasks;
using Unity.Payments.Domain.AccountCodings;
using Unity.Payments.Domain.AccountCodings;
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

        public virtual Task<string> GetAccountDistributionCode(AccountCoding accountCoding)
        {
            string accountDistributionCode = "";
            if (accountCoding != null
				&& accountCoding.Responsibility != null
				&& accountCoding.ServiceLine != null
				&& accountCoding.Stob != null
				&& accountCoding.MinistryClient != null
				&& accountCoding.ProjectNumber != null)
            {
                string accountDistributionPostFix = "000000.0000";
                accountDistributionCode = 
                 $"{accountCoding.MinistryClient}.{accountCoding.Responsibility}.{accountCoding.ServiceLine}.{accountCoding.Stob}.{accountCoding.ProjectNumber}.{accountDistributionPostFix}"; 
            }

            return Task.FromResult(accountDistributionCode);
        }

        public virtual async Task<PaymentConfigurationDto> CreateAsync(CreatePaymentConfigurationDto createUpdatePaymentConfigurationDto)
        {
            PaymentConfiguration? paymentConfiguration = await FindPaymentConfigurationAsync();

            if (paymentConfiguration != null)
            {
                throw new ConfigurationExistsException(L[ErrorConsts.ConfigurationExists]);
            }

            var newPaymentConfiguration = await paymentConfigurationRepository.InsertAsync(new PaymentConfiguration
            (
                createUpdatePaymentConfigurationDto.PaymentThreshold,
                createUpdatePaymentConfigurationDto.PaymentIdPrefix
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
