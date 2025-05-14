using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Payments.Domain.PaymentTags;
using Unity.Payments.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace Unity.Payments.Repositories
{
    public class PaymentTagRepository : EfCoreRepository<PaymentsDbContext, PaymentTag, Guid>, IPaymentTagRepository
    {
        public PaymentTagRepository(IDbContextProvider<PaymentsDbContext> dbContextProvider) : base(dbContextProvider)
        {
        }
        public async Task<List<PaymentTag>> GetTagsByPaymentRequestIdAsync(Guid paymentRequestId)
        {
            var dbSet = await GetDbSetAsync();
            return dbSet.Where(p => p.PaymentRequestId.Equals(paymentRequestId)).ToList();
        }
    }
}
