using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.Payments.Domain.PaymentRequests;
using Unity.Payments.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace Unity.Payments.Repositories
{
    public class PaymentRequestRepository : EfCoreRepository<PaymentsDbContext, PaymentRequest, Guid>, IPaymentRequestRepository
    {
        public PaymentRequestRepository(IDbContextProvider<PaymentsDbContext> dbContextProvider) : base(dbContextProvider)
        {
        }

        public async Task<decimal> GetTotalPaymentRequestAmountByCorrelationIdAsync(Guid correlationId)
        {
            var dbSet = await GetDbSetAsync();
            decimal applicationPaymentRequestsTotal = dbSet
              .Where(p => p.CorrelationId.Equals(correlationId))
              // Need to define a where clause on the Status
              // Don't include declined - right now we don't know how to set status
              .GroupBy(p => p.CorrelationId)
              .Select(p => p.Sum(q => q.Amount))
              .First();

            return applicationPaymentRequestsTotal;
        }

        public override async Task<IQueryable<PaymentRequest>> WithDetailsAsync()
        {
            // Uses the extension method defined above
            return (await GetQueryableAsync()).IncludeDetails();
        }
    }
}
