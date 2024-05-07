using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Payments.Integration.Cas;
using Unity.Payments.PaymentConfigurations;
using Unity.Payment.Shared;
using Unity.Payments.Domain.BatchPaymentRequests;
using Unity.Payments.Domain.PaymentConfigurations;
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
                    payment.PayeeName,
                    payment.ContractNumber,
                    payment.SupplierNumber,
                    payment.SiteId,
                    payment.CorrelationId,
                    payment.Description);

                var response = await _invoiceService.CreateInvoiceByPaymentRequestAsync(paymentRequest);
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

        public async Task<PagedResultDto<PaymentRequestDto>> GetBatchPaymentListAsync(Guid Id)
        {
            var batchPayments = await _batchPaymentRequestsRepository.GetAsync(Id);
            var totalCount = batchPayments.PaymentRequests.Count;

            return new PagedResultDto<PaymentRequestDto>(totalCount, ObjectMapper.Map<List<PaymentRequest>, List<PaymentRequestDto>>(batchPayments.PaymentRequests.ToList()));
        }

        protected virtual async Task<decimal> GetPaymentThresholdAsync()
        {
            var paymentConfigs = await _paymentConfigurationRepository.GetListAsync();

            if (paymentConfigs.Count > 0)
            {
                var paymentConfig = paymentConfigs[0];
                return paymentConfig.PaymentThreshold ?? PaymentSharedConsts.DefaultThresholdAmount;
            }
            return PaymentSharedConsts.DefaultThresholdAmount;
        }

        protected virtual string GetCurrentRequesterName()
        {
            return $"{_currentUser.Name} {_currentUser.SurName}";
        }
    }
}
