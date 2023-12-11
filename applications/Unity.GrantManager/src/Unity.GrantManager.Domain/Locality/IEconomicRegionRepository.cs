using System;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.Locality;

public interface IEconomicRegionRepository : IRepository<EconomicRegion, Guid>
{
}

