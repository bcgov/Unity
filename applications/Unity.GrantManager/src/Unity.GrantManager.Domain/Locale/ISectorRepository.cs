using System;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.Locale;

public interface ISectorRepository : IRepository<Sector, Guid>
{
}

public interface IApplicationSubSectorRepository : IRepository<SubSector, Guid>
{
}
