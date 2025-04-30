using System;
using Volo.Abp.Domain.Repositories;

namespace Unity.Payments.Domain.PaymentThresholds;

public interface IPaymentThresholdRepository : IRepository<PaymentThreshold, Guid>
{

}
