using System;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.Applications;

public interface IApplicationSectorRepository : IRepository<ApplicationSector, Guid>
{
}

public interface IApplicationSubSectorRepository : IRepository<ApplicationSubSector, Guid>
{
}
