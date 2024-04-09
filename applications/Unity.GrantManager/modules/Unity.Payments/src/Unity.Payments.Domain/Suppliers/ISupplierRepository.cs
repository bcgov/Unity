using System;
using Volo.Abp.Domain.Repositories;

namespace Unity.Payments.Suppliers
{
    public interface ISupplierRepository : IRepository<Supplier, Guid>
    {
    }
}
