using System;
using Unity.Payments.EntityFrameworkCore;
using Unity.Payments.Suppliers;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace Unity.Payments.Repositories
{
#pragma warning disable CS8613
    public class SiteRepository : EfCoreRepository<PaymentsDbContext, Site, Guid>, ISiteRepository
    {
        public SiteRepository(IDbContextProvider<PaymentsDbContext> dbContextProvider) : base(dbContextProvider)
        {
        }
    }
#pragma warning restore CS8613
}
