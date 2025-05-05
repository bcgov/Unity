using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.Payments.PaymentTags
{
    public interface IPaymentTagAppService : IApplicationService
    {
        Task<IList<PaymentTagDto>> GetListAsync();
        Task<IList<PaymentTagDto>> GetListWithPaymentRequestIdsAsync(List<Guid> ids);
        Task<PaymentTagDto> CreateorUpdateTagsAsync(Guid id, PaymentTagDto input);
        Task<PaymentTagDto?> GetPaymentTagsAsync(Guid id);
    }
}