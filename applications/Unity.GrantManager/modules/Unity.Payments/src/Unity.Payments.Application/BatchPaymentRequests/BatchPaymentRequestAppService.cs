using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;
using Unity.Payments.PaymentSettings;
using Volo.Abp.Features;
using Volo.Abp.Users;

namespace Unity.Payments.BatchPaymentRequests
{
    [RequiresFeature("Unity.Payments")]
    [Authorize]
    public class BatchPaymentRequestAppService : PaymentsAppService, IBatchPaymentRequestAppService
    {
        private readonly IBatchPaymentRequestRepository _batchPaymentRequestsRepository;
        private readonly ICurrentUser _currentUser;
        private IPaymentSettingsAppService PaymentSettingsAppService { get; set; }

        public BatchPaymentRequestAppService(
            IBatchPaymentRequestRepository batchPaymentRequestsRepository,
            ICurrentUser currentUser,
            IPaymentSettingsAppService paymentSettingsAppService)
        {
            _batchPaymentRequestsRepository = batchPaymentRequestsRepository;
            _currentUser = currentUser;
            PaymentSettingsAppService = paymentSettingsAppService;
        }

        public async Task<BatchPaymentRequestDto> CreateAsync(CreateBatchPaymentRequestDto batchPaymentRequest)
        {
            var paymentThreshold = GetPaymentThresholdSettingValueAsync();

            var newBatchPaymentRequest = new BatchPaymentRequest(Guid.NewGuid(),
                Guid.NewGuid().ToString(), // Need to implement batch number generator
                Enums.PaymentGroup.EFT,
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
                    newBatchPaymentRequest.PaymentGroup,
                    payment.CorrelationId,
                    payment.Description),
                    ConvertPaymentThresholdAmount(paymentThreshold));
            }

            var result = await _batchPaymentRequestsRepository.InsertAsync(newBatchPaymentRequest);

            return ObjectMapper.Map<BatchPaymentRequest, BatchPaymentRequestDto>(result);
        }

        private string GetPaymentThresholdSettingValueAsync()
        {
            PaymentSettingsDto paymentSettingsDto = PaymentSettingsAppService.Get();
            string? thresholdAmount = "";
            if (paymentSettingsDto != null && paymentSettingsDto.PaymentThreshold.HasValue)
            {
                thresholdAmount = paymentSettingsDto.PaymentThreshold.ToString();
            }
            return thresholdAmount != null ? thresholdAmount : "";
        }

        private static decimal ConvertPaymentThresholdAmount(string? paymentThreshold)
        {
            return paymentThreshold == null ? PaymentConsts.DefaultThresholdAmount : decimal.Parse(paymentThreshold);
        }

        private string GetCurrentRequesterName()
        {
            return $"{_currentUser.Name} {_currentUser.SurName}";
        }
    }
}
