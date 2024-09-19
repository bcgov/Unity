using System.Threading.Tasks;
using System;
using Volo.Abp.Application.Services;

namespace Unity.Payments.PaymentInfo
{
    public interface IPaymentInfoAppService : IApplicationService
    {
        Task<PaymentInfoDto> UpdateAsync(Guid id, CreateUpdatePaymentInfoDto input);
    }
}
