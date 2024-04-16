using System;
using Unity.Payments.PaymentConfigurations;
using Unity.Payments.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace Unity.Payments.Repositories
{
    public class PaymentConfigurationRepository : EfCoreRepository<PaymentsDbContext, PaymentConfiguration, Guid>, IPaymentConfigurationRepository
    {
        public PaymentConfigurationRepository(IDbContextProvider<PaymentsDbContext> dbContextProvider) : base(dbContextProvider)
        {
        }
    }
}
