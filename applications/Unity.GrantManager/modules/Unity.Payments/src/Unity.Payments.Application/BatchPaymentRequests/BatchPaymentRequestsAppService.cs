using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;
using Volo.Abp.Features;

namespace Unity.Payments.BatchPaymentRequests
{
    [RequiresFeature("Unity.Payments")]
    [Authorize]
    public class BatchPaymentRequestsAppService : PaymentsAppService, IPaymentRequestsAppService
    {
        public async Task<PaymentsBatchCreatedDto> CreateAsync(CreatePaymentsBatchRequestDto batchPaymentRequest)
        {
            var newBatchPaymentRequest = new BatchPaymentRequest(Guid.NewGuid(), 
                Guid.NewGuid().ToString(),
                Enums.PaymentMethod.EFT,
                batchPaymentRequest.Description,
                batchPaymentRequest.Provider);

            foreach (var payment in newBatchPaymentRequest.PaymentRequests)
            {
                newBatchPaymentRequest.AddPaymentRequest(new PaymentRequest(
                    Guid.NewGuid(),
                    newBatchPaymentRequest,
                    payment.Amount,
                    newBatchPaymentRequest.Method,
                    payment.CorrelationId,
                    payment.Description));
            }
            
            return await Task.FromResult(new PaymentsBatchCreatedDto());
        }
    }
}
