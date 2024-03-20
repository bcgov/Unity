using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.Payments.BatchPaymentRequests
{
    public interface IPaymentRequestsAppService : IApplicationService
    {
        Task<PaymentsBatchCreatedDto> CreateAsync(CreatePaymentsBatchRequestDto batchPaymentRequest);
    }
}
