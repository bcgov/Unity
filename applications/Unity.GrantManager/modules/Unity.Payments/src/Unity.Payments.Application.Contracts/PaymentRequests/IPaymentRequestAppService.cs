using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace Unity.Payments.PaymentRequests
{
    public interface IPaymentRequestAppService : IApplicationService
    {
        Task<List<PaymentRequestDto>> CreateAsync(List<CreatePaymentRequestDto> paymentRequests);
        Task<PagedResultDto<PaymentRequestDto>> GetListAsync(PagedAndSortedResultRequestDto input);
        Task<decimal> GetTotalPaymentRequestAmountByCorrelationIdAsync(Guid correlationId);
        Task<List<PaymentDetailsDto>> GetListByApplicationIdAsync(Guid applicationId);
        Task<List<PaymentDetailsDto>> GetListByPaymentIdsAsync(List<Guid> paymentIds);

        Task<List<PaymentRequestDto>> UpdateStatusAsync(List<UpdatePaymentStatusRequestDto> paymentRequests);

    }
}
