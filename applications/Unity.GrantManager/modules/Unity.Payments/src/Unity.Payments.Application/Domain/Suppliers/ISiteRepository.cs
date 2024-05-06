using System;
using Volo.Abp.Domain.Repositories;

namespace Unity.Payments.Domain.Suppliers
{
    public interface ISiteRepository : IReadOnlyBasicRepository<Site, Guid>
    {
    }
}
