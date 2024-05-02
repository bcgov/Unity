using System;
using Unity.Payments.Domain.Suppliers;
using Unity.Payments.EntityFrameworkCore;
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
