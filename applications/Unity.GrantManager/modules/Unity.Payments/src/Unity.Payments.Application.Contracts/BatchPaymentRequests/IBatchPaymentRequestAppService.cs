using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace Unity.Payments.BatchPaymentRequests
{
    public interface IBatchPaymentRequestAppService : IApplicationService
    {
        Task<BatchPaymentRequestDto> CreateAsync(CreateBatchPaymentRequestDto batchPaymentRequest);

        Task<PagedResultDto<BatchPaymentRequestDto>> GetListAsync(PagedAndSortedResultRequestDto input);

        Task<PagedResultDto<PaymentRequestDto>> GetBatchPaymentListAsync(Guid Id);
    }
}
