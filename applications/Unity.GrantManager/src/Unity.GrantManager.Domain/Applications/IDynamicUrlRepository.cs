using System;
using Unity.GrantManager.Integrations;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.Applications;

public interface IDynamicUrlRepository : IRepository<DynamicUrl, Guid>
{
    
}
