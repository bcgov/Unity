using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;
using Unity.Payments.Settings;
using Volo.Abp.Features;
using Volo.Abp.Users;

namespace Unity.Payments.BatchPaymentRequests
{
    [RequiresFeature("Unity.Payments")]
    [Authorize]
    public class BatchPaymentRequestAppService : PaymentsAppService, IBatchPaymentRequestAppService
    {
        private readonly IBatchPaymentRequestsRepository _batchPaymentRequestsRepository;
        private readonly ICurrentUser _currentUser;

        public BatchPaymentRequestAppService(IBatchPaymentRequestsRepository batchPaymentRequestsRepository,
            ICurrentUser currentUser)
        {
            _batchPaymentRequestsRepository = batchPaymentRequestsRepository;
            _currentUser = currentUser;
        }

        public async Task<BatchPaymentRequestDto> CreateAsync(CreateBatchPaymentRequestDto batchPaymentRequest)
        {
            var paymentThreshold = await GetPaymentThresholdSettingValueAsync();

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

        private async Task<string?> GetPaymentThresholdSettingValueAsync()
        {
            return await SettingProvider.GetOrNullAsync(PaymentsSettings.PaymentThreshold);
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
