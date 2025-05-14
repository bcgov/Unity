using System;
using Unity.Payments.Domain.PaymentThresholds;
using Unity.Payments.PaymentThresholds;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.Payments
{    
    public class PaymentThresholdAppService :
            CrudAppService<
            PaymentThreshold,
            PaymentThresholdDto,
            Guid,
            PagedAndSortedResultRequestDto,
            UpdatePaymentThresholdDto>, IPaymentThresholdAppService
    {
        public PaymentThresholdAppService(IRepository<PaymentThreshold, Guid> repository)
            : base(repository)
        {
        }
    }
}
