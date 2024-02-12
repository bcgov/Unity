using System;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.Locality;

public interface ICommunityRepository : IRepository<Community, Guid>
{
}

