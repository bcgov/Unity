using System;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace Unity.Payments.Domain.Suppliers
{
    public interface ISupplierRepository : IBasicRepository<Supplier, Guid>
    {
        Task<Supplier?> GetBySupplierNumberAsync(string supplierNumber, bool includeDetails = false);
    }
}
