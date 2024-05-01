using System;
using Volo.Abp.Domain.Repositories;

namespace Unity.Payments.Domain.PaymentConfigurations;

public interface IPaymentConfigurationRepository : IRepository<PaymentConfiguration, Guid>
{

}
