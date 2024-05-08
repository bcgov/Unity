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

        public override async Task<IQueryable<PaymentRequest>> WithDetailsAsync()
        {
            // Uses the extension method defined above
            return (await GetQueryableAsync()).IncludeDetails();
        }
    }
}
