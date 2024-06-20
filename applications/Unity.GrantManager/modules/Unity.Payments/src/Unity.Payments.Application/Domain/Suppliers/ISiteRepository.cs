using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace Unity.Payments.Domain.Suppliers
{
    public interface ISiteRepository : IBasicRepository<Site, Guid>
    {
        Task<List<Site>> GetBySupplierAsync(Guid supplierId);
    }
}
