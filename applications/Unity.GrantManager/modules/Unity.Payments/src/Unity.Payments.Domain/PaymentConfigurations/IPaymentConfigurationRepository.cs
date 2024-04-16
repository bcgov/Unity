using System;
using Volo.Abp.Domain.Repositories;

namespace Unity.Payments.PaymentConfigurations;

public interface IPaymentConfigurationRepository : IRepository<PaymentConfiguration, Guid>
{

}
