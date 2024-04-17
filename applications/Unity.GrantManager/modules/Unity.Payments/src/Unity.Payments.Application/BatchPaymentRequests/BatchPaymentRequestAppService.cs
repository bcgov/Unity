using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;
using Unity.Payments.Integration.Cas;
using Unity.Payments.PaymentConfigurations;
using Volo.Abp.Features;
using Volo.Abp.Users;

namespace Unity.Payments.BatchPaymentRequests
{
    [RequiresFeature("Unity.Payments")]
    [Authorize]
    public class BatchPaymentRequestAppService : PaymentsAppService, IBatchPaymentRequestAppService
    {
        private readonly IBatchPaymentRequestRepository _batchPaymentRequestsRepository;
        private readonly InvoiceService _invoiceService;
        private readonly IPaymentConfigurationRepository _paymentConfigurationRepository;
        private readonly ICurrentUser _currentUser;

        public BatchPaymentRequestAppService(
            IBatchPaymentRequestRepository batchPaymentRequestsRepository,
            IPaymentConfigurationRepository paymentConfigurationRepository,
            InvoiceService invoiceService,
            ICurrentUser currentUser)
        {
            _paymentConfigurationRepository = paymentConfigurationRepository;
            _batchPaymentRequestsRepository = batchPaymentRequestsRepository;
            _currentUser = currentUser;
            _invoiceService = invoiceService;
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
                PaymentRequest paymentRequest = new PaymentRequest(
                    Guid.NewGuid(),
                    newBatchPaymentRequest,
                    payment.InvoiceNumber,
                    payment.Amount,
                    payment.SiteId,
                    payment.CorrelationId,
                    payment.Description);

                var response = await _invoiceService.CreateInvoiceByPaymentRequestAsync(paymentRequest);
            }

            var result = await _batchPaymentRequestsRepository.InsertAsync(newBatchPaymentRequest);            
            return ObjectMapper.Map<BatchPaymentRequest, BatchPaymentRequestDto>(result);
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
