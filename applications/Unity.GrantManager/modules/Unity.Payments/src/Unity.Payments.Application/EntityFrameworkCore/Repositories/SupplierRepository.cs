using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.Payments.Domain.Suppliers;
using Unity.Payments.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace Unity.Payments.Repositories
{
    public class SupplierRepository : EfCoreRepository<PaymentsDbContext, Supplier, Guid>, ISupplierRepository
    {
        public SupplierRepository(IDbContextProvider<PaymentsDbContext> dbContextProvider) : base(dbContextProvider)
        {
        }

        public async Task<Supplier?> GetByCorrelationAsync(Guid correlationId, string correlationProvider, bool includeDetails = false)
        {
            var dbSet = await GetDbSetAsync();
            return await dbSet
                    .IncludeDetails(includeDetails)
                    .FirstOrDefaultAsync(s => s.CorrelationId == correlationId
                        && s.CorrelationProvider == correlationProvider);
        }

        public override async Task<IQueryable<Supplier>> WithDetailsAsync()
        {
            // Uses the extension method defined above
            return (await GetQueryableAsync()).IncludeDetails();
        }
    }
}
