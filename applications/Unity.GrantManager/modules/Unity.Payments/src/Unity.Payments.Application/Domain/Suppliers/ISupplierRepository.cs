using System;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace Unity.Payments.Domain.Suppliers
{
    public interface ISupplierRepository : IBasicRepository<Supplier, Guid>
    {
        Task<Supplier?> GetByCorrelationAsync(Guid correlationId, string correlationProvider, bool includeDetails = false);
    }
}
