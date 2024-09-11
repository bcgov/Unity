using System.Collections.Generic;
using System.Threading;
using System;
using System.Threading.Tasks;
using Volo.Abp.AuditLogging;

namespace Unity.GrantManager.Applications;

public interface IExtendedAuditLogRepository : IAuditLogRepository
{
    Task<List<EntityChangeWithUsername>> GetEntityChangeByTypeWithUsernameAsync(Guid? entityId, List<string> entityTypeFullNames, CancellationToken cancellationToken);
}
