using System;
using Unity.Payments.PaymentThresholds;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.Payments
{
    public interface IPaymentThresholdAppService : ICrudAppService<
            PaymentThresholdDto,
            Guid,
            PagedAndSortedResultRequestDto,
            UpdatePaymentThresholdDto>
    {
    }
}
