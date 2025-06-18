using System.Threading.Tasks;
using Unity.Payments.Domain.AccountCodings;
using Unity.Payments.Domain.Exceptions;
using Unity.Payments.Domain.PaymentConfigurations;
using Volo.Abp.Features;

namespace Unity.Payments.PaymentConfigurations
{
    [RequiresFeature("Unity.Payments")]
    public class PaymentConfigurationAppService : PaymentsAppService, IPaymentConfigurationAppService
    {
        private readonly IPaymentConfigurationRepository _paymentConfigurationRepository;

        public PaymentConfigurationAppService(IPaymentConfigurationRepository paymentConfigurationRepository)
        {
            _paymentConfigurationRepository = paymentConfigurationRepository;
        }

        public virtual async Task<PaymentConfigurationDto?> GetAsync()
        {
            PaymentConfiguration? paymentConfiguration = await FindPaymentConfigurationAsync();

            if (paymentConfiguration == null) { return null; }

            return ObjectMapper.Map<PaymentConfiguration, PaymentConfigurationDto>(paymentConfiguration);
        }

        public virtual async Task<string?> GetAccountDistributionCodeAsync()
        {
            PaymentConfiguration? paymentConfiguration = await FindPaymentConfigurationAsync();
            string accountDistributionCode = "";
            if (paymentConfiguration != null
				&& paymentConfiguration.Responsibility != null
				&& paymentConfiguration.ServiceLine != null
				&& paymentConfiguration.Stob != null
				&& paymentConfiguration.MinistryClient != null
				&& paymentConfiguration.ProjectNumber != null)
            {
                string accountDistributionPostFix = "000000.0000";
                accountDistributionCode = 
                 $"{paymentConfiguration.MinistryClient}.{paymentConfiguration.Responsibility}.{paymentConfiguration.ServiceLine}.{paymentConfiguration.Stob}.{paymentConfiguration.ProjectNumber}.{accountDistributionPostFix}"; 
            }

            return accountDistributionCode;
        }

        public virtual async Task<PaymentConfigurationDto> CreateAsync(CreatePaymentConfigurationDto createPaymentConfigurationDto)
        {
            PaymentConfiguration? paymentConfiguration = await FindPaymentConfigurationAsync();

            if (paymentConfiguration != null)
            {
                throw new ConfigurationExistsException(L[ErrorConsts.ConfigurationExists]);
            }

            var newPaymentConfiguration = await _paymentConfigurationRepository.InsertAsync(new PaymentConfiguration
            (
                createPaymentConfigurationDto.PaymentThreshold,
                createPaymentConfigurationDto.PaymentIdPrefix,
                AccountCoding.Create(
                    createPaymentConfigurationDto.MinistryClient,
                    createPaymentConfigurationDto.Responsibility,
                    createPaymentConfigurationDto.ServiceLine,
                    createPaymentConfigurationDto.Stob,
                    createPaymentConfigurationDto.ProjectNumber
                )
            ));

            return ObjectMapper.Map<PaymentConfiguration, PaymentConfigurationDto>(newPaymentConfiguration);
        }

        public virtual async Task<PaymentConfigurationDto> UpdateAsync(UpdatePaymentConfigurationDto updatePaymentConfigurationDto)
        {
            PaymentConfiguration? paymentConfiguration = await FindPaymentConfigurationAsync() ??
                throw new ConfigurationExistsException(L[ErrorConsts.ConfigurationDoesNotExist]);

            paymentConfiguration.PaymentThreshold = updatePaymentConfigurationDto.PaymentThreshold;
            paymentConfiguration.PaymentIdPrefix = updatePaymentConfigurationDto.PaymentIdPrefix;

            paymentConfiguration.SetAccountCoding(AccountCoding.Create(updatePaymentConfigurationDto.MinistryClient,
                updatePaymentConfigurationDto.Responsibility,
                updatePaymentConfigurationDto.ServiceLine,
                updatePaymentConfigurationDto.Stob,
                updatePaymentConfigurationDto.ProjectNumber));

            var updatedConfiguration = await _paymentConfigurationRepository.UpdateAsync(paymentConfiguration);

            return ObjectMapper.Map<PaymentConfiguration, PaymentConfigurationDto>(updatedConfiguration);
        }
        

        protected virtual async Task<PaymentConfiguration?> FindPaymentConfigurationAsync()
        {
            var paymentConfigurations = await _paymentConfigurationRepository.GetListAsync();
            var paymentConfiguration = paymentConfigurations.Count > 0 ? paymentConfigurations[0] : null;
            return paymentConfiguration;
        }
    }
}
