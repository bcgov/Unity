using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.Payments.PaymentRequests
{
    public interface IPaymentBulkActionsAppService : IApplicationService
    {
        Task<StorePaymentIdsResultDto> StorePaymentIdsAsync(StorePaymentIdsRequestDto input);
    }
}
