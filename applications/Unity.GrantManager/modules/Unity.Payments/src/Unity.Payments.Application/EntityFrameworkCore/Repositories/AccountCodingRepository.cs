using System;
using Unity.Payments.Domain.AccountCodings;
using Unity.Payments.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace Unity.Payments.Repositories
{
    public class AccountCodingRepository : EfCoreRepository<PaymentsDbContext, AccountCoding, Guid>, IAccountCodingRepository
    {
        public AccountCodingRepository(IDbContextProvider<PaymentsDbContext> dbContextProvider) : base(dbContextProvider)
        {
        }
    }
}
