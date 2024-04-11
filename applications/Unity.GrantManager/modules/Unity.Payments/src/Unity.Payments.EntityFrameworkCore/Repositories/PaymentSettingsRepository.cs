using System;
using Unity.Payments.PaymentSettings;
using Unity.Payments.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;


namespace Unity.Payments.Repositories
{
#pragma warning disable CS8613
	public class PaymentSettingsRepository : EfCoreRepository<PaymentsDbContext, PaymentSetting, Guid>, IPaymentSettingsRepository
    {
        public PaymentSettingsRepository(IDbContextProvider<PaymentsDbContext> dbContextProvider) : base(dbContextProvider)
        {
        }
    }
#pragma warning restore CS8613
}
