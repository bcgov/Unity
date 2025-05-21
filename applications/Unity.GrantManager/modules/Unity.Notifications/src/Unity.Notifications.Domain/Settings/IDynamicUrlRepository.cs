using System;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.Notifications.Settings;

public interface IDynamicUrlRepository : IRepository<DynamicUrl, Guid>
{
    
}
