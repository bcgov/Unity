using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Payments.BatchPaymentRequests;
using Unity.Payments.EntityFrameworkCore;
using Unity.Payments.Suppliers;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace Unity.Payments.Repositories
{
#pragma warning disable CS8613
    public class SupplierRepository : EfCoreRepository<PaymentsDbContext, Supplier, Guid>, ISupplierRepository
    {
        public SupplierRepository(IDbContextProvider<PaymentsDbContext> dbContextProvider) : base(dbContextProvider)
        {
        }

        public override async Task<IQueryable<Supplier>> WithDetailsAsync()
        {
            // Uses the extension method defined above
            return (await GetQueryableAsync()).IncludeDetails();
        }
    }
#pragma warning restore CS8613
}
