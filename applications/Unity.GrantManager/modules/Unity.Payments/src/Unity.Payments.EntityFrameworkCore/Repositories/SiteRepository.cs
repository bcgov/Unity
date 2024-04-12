using System;
using Unity.Payments.EntityFrameworkCore;
using Unity.Payments.Suppliers;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace Unity.Payments.Repositories
{
    public class SiteRepository : EfCoreRepository<PaymentsDbContext, Site, Guid>, ISiteRepository
    {
        public SiteRepository(IDbContextProvider<PaymentsDbContext> dbContextProvider) : base(dbContextProvider)
        {
        }
    }
}
