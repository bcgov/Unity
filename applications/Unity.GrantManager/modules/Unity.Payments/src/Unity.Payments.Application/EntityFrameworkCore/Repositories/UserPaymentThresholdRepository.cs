using System;
using Unity.Payments.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;
using Unity.Payments.Domain.PaymentThresholds;

namespace Unity.Payments.Repositories
{
    public class PaymentThresholdRepository : EfCoreRepository<PaymentsDbContext, PaymentThreshold, Guid>, IPaymentThresholdRepository
    {
        public PaymentThresholdRepository(IDbContextProvider<PaymentsDbContext> dbContextProvider) : base(dbContextProvider)
        {
        }
    }
}
