using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

        public async Task<List<Site>> GetBySupplierAsync(Guid supplierId)
        {
            var dbSet = await GetDbSetAsync();

            return dbSet.Where(s => s.SupplierId == supplierId).ToList();
        }
    }
}
