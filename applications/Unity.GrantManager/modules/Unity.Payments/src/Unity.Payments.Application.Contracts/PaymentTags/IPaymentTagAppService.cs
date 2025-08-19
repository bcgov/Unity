using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace Unity.Payments.PaymentTags;

public interface IPaymentTagAppService : IApplicationService
{
    Task<IList<PaymentTagDto>> GetListAsync();
    Task<IList<PaymentTagDto>> GetListWithPaymentRequestIdsAsync(List<Guid> ids);
    Task<List<PaymentTagDto>> AssignTagsAsync(AssignPaymentTagDto input);
    Task<PaymentTagDto?> GetPaymentTagsAsync(Guid id);
    Task<PagedResultDto<TagSummaryCountDto>> GetTagSummaryAsync();
    Task DeleteTagAsync(Guid id);
    Task DeleteTagWithTagIdAsync(Guid tagId);
}