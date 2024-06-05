using System;
using Volo.Abp.Domain.Repositories;

namespace Unity.Payments.Domain.Suppliers
{
    public interface ISiteRepository : IRepository<Site, Guid>
    {
    }
}
