using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.Payments.Domain.BatchPaymentRequests;
using Unity.Payments.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace Unity.Payments.Repositories
{
    public class BatchPaymentRequestRepository : EfCoreRepository<PaymentsDbContext, BatchPaymentRequest, Guid>, IBatchPaymentRequestRepository
    {
        public BatchPaymentRequestRepository(IDbContextProvider<PaymentsDbContext> dbContextProvider) : base(dbContextProvider)
        {
        }

        public override async Task<IQueryable<BatchPaymentRequest>> WithDetailsAsync()
        {
            // Uses the extension method defined above
            return (await GetQueryableAsync()).IncludeDetails();
        }
    }
}
