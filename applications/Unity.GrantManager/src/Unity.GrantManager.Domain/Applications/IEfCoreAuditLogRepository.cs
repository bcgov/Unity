using System.Collections.Generic;
using System.Threading;
using System;
using System.Threading.Tasks;
using Volo.Abp.AuditLogging;
using Volo.Abp.Auditing;

namespace Unity.GrantManager.Applications;

public interface IExtendedAuditLogRepository : IAuditLogRepository
{
    Task<List<EntityChangeWithUsername>> GetEntityChangeByTypeWithUsernameAsync(Guid? entityId, List<string> entityTypeFullNames, CancellationToken cancellationToken);

    Task<(long TotalCount, List<AuditLogEntityChange> Items)> GetEntityChangePageAsync(
        DateTime? startTime,
        DateTime? endTime,
        string? entityTypeFullName,
        EntityChangeType? changeType,
        string? serviceName,
        string? methodName,
        string? filter,
        string? sorting,
        string? propertyName,
        IReadOnlyCollection<string>? entityIds,
        int skipCount,
        int maxResultCount,
        CancellationToken cancellationToken = default);

    Task<List<string>> GetEntityTypeFullNamesAsync(
        DateTime? startTime,
        DateTime? endTime,
        CancellationToken cancellationToken = default);

    Task<int> DeleteOlderThanAsync(DateTime cutoffDate, CancellationToken cancellationToken = default);
}
