﻿using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using Unity.Payments.Exceptions;
using Volo.Abp.Features;

namespace Unity.Payments.PaymentConfigurations
{
    [RequiresFeature("Unity.Payments")]
    [Authorize]
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