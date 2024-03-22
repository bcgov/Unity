using System;
using Unity.Payments.BatchPaymentRequests;
using Unity.Payments.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace Unity.Payments.Repositories
{
#pragma warning disable CS8613
    public class BatchPaymentRequestsRepository : EfCoreRepository<PaymentsDbContext, BatchPaymentRequest, Guid>, IBatchPaymentRequestsRepository
    {
        public BatchPaymentRequestsRepository(IDbContextProvider<PaymentsDbContext> dbContextProvider) : base(dbContextProvider)
        {
        }
    }
#pragma warning restore CS8613
}
