using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;
using Volo.Abp.Features;

namespace Unity.Payments.BatchPaymentRequests
{
    [RequiresFeature("Unity.Payments")]
    [Authorize]
    public class BatchPaymentRequestAppService : PaymentsAppService, IBatchPaymentRequestAppService
    {
        private readonly IBatchPaymentRequestsRepository _batchPaymentRequestsRepository;

        public BatchPaymentRequestAppService(IBatchPaymentRequestsRepository batchPaymentRequestsRepository)
        {
            _batchPaymentRequestsRepository = batchPaymentRequestsRepository;
        }

        public async Task<BatchPaymentRequestDto> CreateAsync(CreateBatchPaymentRequestDto batchPaymentRequest)
        {
            var newBatchPaymentRequest = new BatchPaymentRequest(Guid.NewGuid(),
                Guid.NewGuid().ToString(),
                Enums.PaymentGroup.EFT,
                batchPaymentRequest.Description,
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
                    payment.Description));                
            }

            var result = await _batchPaymentRequestsRepository.InsertAsync(newBatchPaymentRequest);

            return ObjectMapper.Map<BatchPaymentRequest, BatchPaymentRequestDto>(result);
        }
    }
}
