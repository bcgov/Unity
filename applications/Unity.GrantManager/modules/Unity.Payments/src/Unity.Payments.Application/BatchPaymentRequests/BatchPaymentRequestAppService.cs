using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Payments.PaymentConfigurations;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Features;
using Volo.Abp.Users;

namespace Unity.Payments.BatchPaymentRequests
{
    [RequiresFeature("Unity.Payments")]
    [Authorize]
    public class BatchPaymentRequestAppService : PaymentsAppService, IBatchPaymentRequestAppService
    {
        private readonly IBatchPaymentRequestRepository _batchPaymentRequestsRepository;
        private readonly IPaymentConfigurationRepository _paymentConfigurationRepository;
        private readonly ICurrentUser _currentUser;

        public BatchPaymentRequestAppService(IPaymentConfigurationRepository paymentConfigurationRepository,
            IBatchPaymentRequestRepository batchPaymentRequestsRepository,
            ICurrentUser currentUser)
        {
            _paymentConfigurationRepository = paymentConfigurationRepository;
            _batchPaymentRequestsRepository = batchPaymentRequestsRepository;
            _currentUser = currentUser;
        }

        public virtual async Task<BatchPaymentRequestDto> CreateAsync(CreateBatchPaymentRequestDto batchPaymentRequest)
        {
            var newBatchPaymentRequest = new BatchPaymentRequest(Guid.NewGuid(),
                Guid.NewGuid().ToString(), // Need to implement batch number generator
                batchPaymentRequest.Description,
                GetCurrentRequesterName(),
                batchPaymentRequest.Provider);

            foreach (var payment in batchPaymentRequest.PaymentRequests)
            {
                newBatchPaymentRequest.AddPaymentRequest(new PaymentRequest(
                    Guid.NewGuid(),
                    newBatchPaymentRequest,
                    payment.InvoiceNumber,
                    payment.Amount,
                    payment.SiteId,
                    payment.CorrelationId,
                    payment.Description),
                    await GetPaymentThresholdAsync());
            }

            var result = await _batchPaymentRequestsRepository.InsertAsync(newBatchPaymentRequest);

            return ObjectMapper.Map<BatchPaymentRequest, BatchPaymentRequestDto>(result);
        }
        public async Task<PagedResultDto<BatchPaymentRequestDto>> GetListAsync(PagedAndSortedResultRequestDto input)
        {
            var batchPayments = await _batchPaymentRequestsRepository.GetPagedListAsync(input.SkipCount, input.MaxResultCount, input.Sorting ?? string.Empty, true);
            var totalCount = await _batchPaymentRequestsRepository.GetCountAsync();

            return new PagedResultDto<BatchPaymentRequestDto>(totalCount, ObjectMapper.Map<List<BatchPaymentRequest>, List<BatchPaymentRequestDto>>(batchPayments));
        }
        protected virtual async Task<decimal> GetPaymentThresholdAsync()
        {
            var paymentConfigs = await _paymentConfigurationRepository.GetListAsync();

            if (paymentConfigs.Count > 0)
            {
                var paymentConfig = paymentConfigs[0];
                return paymentConfig.PaymentThreshold ?? PaymentConsts.DefaultThresholdAmount;
            }
            return PaymentConsts.DefaultThresholdAmount;
        }

        protected virtual string GetCurrentRequesterName()
        {
            return $"{_currentUser.Name} {_currentUser.SurName}";
        }
    }
}
