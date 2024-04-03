using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.Payments.BatchPaymentRequests
{
    public interface IBatchPaymentRequestAppService : IApplicationService
    {
        Task<BatchPaymentRequestDto> CreateAsync(CreateBatchPaymentRequestDto batchPaymentRequest);
    }
}
