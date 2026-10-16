using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.Modules.Shared.Constants;
using Volo.Abp.AuditLogging;
using Volo.Abp.AuditLogging.EntityFrameworkCore;
using Volo.Abp.Auditing;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.Identity;

namespace Unity.GrantManager.Repositories
{
    [ExposeServices(typeof(IExtendedAuditLogRepository))]
    public class ExtendedEfCoreAuditLogRepository(IDbContextProvider<IAuditLoggingDbContext> dbContextProvider,
        IIdentityUserRepository identityUserRepository) : EfCoreAuditLogRepository(dbContextProvider), IExtendedAuditLogRepository
    {
        private const int MaxAuditPageSize = 100;

        public virtual async Task<List<EntityChangeWithUsername>> GetEntityChangeByTypeWithUsernameAsync(Guid? entityId, List<string> entityTypeFullNames, CancellationToken cancellationToken)
        {
            List<EntityChangeWithUsername> entities = [];
            var usernameCache = new Dictionary<Guid, string>();
            var dbSet = await GetDbSetAsync();
            var dqQuery = dbSet.AsNoTracking().IncludeDetails().Where(x => x.EntityChanges.Any(y =>
                entityTypeFullNames.Contains(y.EntityTypeFullName)
                && (entityId == null || entityId == Guid.Empty || y.EntityId == entityId.ToString())));

            if (entityId != null && entityId != Guid.Empty)
            {
                dqQuery = dbSet.AsNoTracking().IncludeDetails().Where(x => x.EntityChanges.Any(y =>
                    entityTypeFullNames.Contains(y.EntityTypeFullName)
                    && y.EntityId == entityId.ToString()));
            }
            var auditLogs = await dqQuery.Distinct().ToListAsync(GetCancellationToken(cancellationToken));

            foreach (var auditLog in auditLogs)
            {
                foreach (var entityChange in auditLog.EntityChanges.Where(y =>
                    entityTypeFullNames.Contains(y.EntityTypeFullName)
                    && (entityId == null || entityId == Guid.Empty || y.EntityId == entityId.ToString())))
                {
                    string userName = "";
                    if (Guid.TryParse(auditLog.UserId.ToString(), out Guid userId))
                    {
                        if (!usernameCache.TryGetValue(userId, out userName!))
                        {
                            userName = await ResolveUsername(userId);
                            usernameCache[userId] = userName;
                        }
                    }
                    entities.Add(
                        new EntityChangeWithUsername()
                        {
                            UserName = userName,
                            EntityChange = entityChange
                        }
                    );
                }
            }

            return entities;
        }

        public virtual async Task<(long TotalCount, List<AuditLogEntityChange> Items)> GetEntityChangePageAsync(
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
            CancellationToken cancellationToken = default)
        {
            skipCount = Math.Max(skipCount, 0);
            maxResultCount = Math.Clamp(maxResultCount, 1, MaxAuditPageSize);

            var dbSet = await GetDbSetAsync();
            var query = dbSet.AsNoTracking()
                .WhereIf(!string.IsNullOrWhiteSpace(entityTypeFullName), x => x.EntityChanges.Any(y => y.EntityTypeFullName == entityTypeFullName))
                .WhereIf(!string.IsNullOrWhiteSpace(serviceName), x => x.Actions.Any(y => y.ServiceName != null && y.ServiceName.Contains(serviceName!)))
                .WhereIf(!string.IsNullOrWhiteSpace(methodName), x => x.Actions.Any(y => y.MethodName != null && y.MethodName.Contains(methodName!)))
                .WhereIf(!string.IsNullOrWhiteSpace(filter), x =>
                    x.Url != null && x.Url.Contains(filter!) ||
                    x.UserName != null && x.UserName.Contains(filter!) ||
                    x.EntityChanges.Any(y => y.EntityId.Contains(filter!) || y.EntityTypeFullName.Contains(filter!) ||
                        entityIds != null && entityIds.Contains(y.EntityTypeFullName + ":" + y.EntityId)))
                .SelectMany(x => x.EntityChanges
                    .Where(y => !startTime.HasValue || y.ChangeTime >= startTime.Value)
                    .Where(y => !endTime.HasValue || y.ChangeTime <= endTime.Value)
                    .Where(y => !changeType.HasValue || y.ChangeType == changeType.Value)
                    .Where(y => string.IsNullOrWhiteSpace(entityTypeFullName) || y.EntityTypeFullName == entityTypeFullName)
                    .Where(y => string.IsNullOrWhiteSpace(filter)
                        || (x.Url != null && x.Url.Contains(filter!))
                        || (x.UserName != null && x.UserName.Contains(filter!))
                        || y.EntityId.Contains(filter!)
                        || y.EntityTypeFullName.Contains(filter!)
                        || entityIds != null && entityIds.Contains(y.EntityTypeFullName + ":" + y.EntityId))
                    .Where(y => string.IsNullOrWhiteSpace(propertyName)
                        || y.PropertyChanges.Any(p => p.PropertyName.Contains(propertyName!)))
                    .SelectMany(y => y.PropertyChanges.Select(p => new AuditLogEntityChange
                    {
                        AuditLogId = x.Id,
                        EntityChangeId = y.Id,
                        ChangeTime = y.ChangeTime,
                        ExecutionTime = x.ExecutionTime,
                        EntityTypeFullName = y.EntityTypeFullName,
                        EntityId = y.EntityId,
                        PropertyName = p.PropertyName,
                        OriginalValue = p.OriginalValue,
                        NewValue = p.NewValue,
                        ChangeType = y.ChangeType,
                        UserId = x.UserId,
                        UserName = x.UserName,
                        TenantName = x.TenantName,
                        ServiceName = x.Actions
                            .OrderBy(a => a.ExecutionTime)
                            .Select(a => a.ServiceName)
                            .FirstOrDefault(),
                        MethodName = x.Actions
                            .OrderBy(a => a.ExecutionTime)
                            .Select(a => a.MethodName)
                            .FirstOrDefault(),
                        Url = x.Url,
                        HttpMethod = x.HttpMethod,
                        HttpStatusCode = x.HttpStatusCode
                    })));

            var totalCount = await query.LongCountAsync(GetCancellationToken(cancellationToken));
            var userOrder = await GetUserOrderAsync(sorting, cancellationToken);
            var sortedQuery = userOrder != null
                ? ApplyUserSorting(query, userOrder, sorting!)
                : ApplySorting(query, sorting);
            var items = await sortedQuery
                .Skip(skipCount)
                .Take(maxResultCount)
                .ToListAsync(GetCancellationToken(cancellationToken));

            return (totalCount, items);
        }

        private static bool IsUserSorting(string? sorting, out bool bySurname, out bool descending)
        {
            var parts = (sorting ?? string.Empty).Trim().ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var field = parts.Length > 0 ? parts[0] : string.Empty;
            bySurname = field == "usersurname";
            descending = parts.Length > 1 && parts[1] == "desc";
            return field is "userfirstname" or "usersurname";
        }

        // Identity users live in a different DbContext, so users are ranked in memory and the rank is translated to array_position.
        private async Task<Guid[]?> GetUserOrderAsync(string? sorting, CancellationToken cancellationToken)
        {
            if (!IsUserSorting(sorting, out var bySurname, out _))
            {
                return null;
            }

            var users = await identityUserRepository.GetListAsync(
                sorting: null, maxResultCount: int.MaxValue, skipCount: 0, includeDetails: false,
                cancellationToken: GetCancellationToken(cancellationToken));

            var ordered = bySurname
                ? users.OrderBy(u => u.Surname ?? string.Empty, StringComparer.OrdinalIgnoreCase).ThenBy(u => u.Name ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                : users.OrderBy(u => u.Name ?? string.Empty, StringComparer.OrdinalIgnoreCase).ThenBy(u => u.Surname ?? string.Empty, StringComparer.OrdinalIgnoreCase);

            return ordered.Select(u => u.Id).ToArray();
        }

        private static IOrderedQueryable<AuditLogEntityChange> ApplyUserSorting(
            IQueryable<AuditLogEntityChange> query,
            Guid[] userOrder,
            string sorting)
        {
            IsUserSorting(sorting, out _, out var descending);
            return descending
                ? query.OrderByDescending(x => x.UserId == null ? -1 : Array.IndexOf(userOrder, x.UserId.Value)).ThenByDescending(x => x.ChangeTime)
                : query.OrderBy(x => x.UserId == null ? -1 : Array.IndexOf(userOrder, x.UserId.Value)).ThenByDescending(x => x.ChangeTime);
        }

        private static IOrderedQueryable<AuditLogEntityChange> ApplySorting(
            IQueryable<AuditLogEntityChange> query,
            string? sorting)
        {
            return sorting?.Trim().ToLowerInvariant() switch
            {
                "changetime asc" => query.OrderBy(x => x.ChangeTime),
                "entitytypefullname asc" => query.OrderBy(x => x.EntityTypeFullName),
                "propertyname asc" => query.OrderBy(x => x.PropertyName),
                "changetype asc" => query.OrderBy(x => x.ChangeType),
                "username asc" => query.OrderBy(x => x.UserName),
                "changetime desc" => query.OrderByDescending(x => x.ChangeTime),
                "entitytypefullname desc" => query.OrderByDescending(x => x.EntityTypeFullName),
                "propertyname desc" => query.OrderByDescending(x => x.PropertyName),
                "changetype desc" => query.OrderByDescending(x => x.ChangeType),
                "username desc" => query.OrderByDescending(x => x.UserName),
                _ => query.OrderByDescending(x => x.ChangeTime)
            };
        }

        public virtual async Task<List<string>> GetEntityTypeFullNamesAsync(
            DateTime? startTime,
            DateTime? endTime,
            CancellationToken cancellationToken = default)
        {
            var dbSet = await GetDbSetAsync();

            return await dbSet.AsNoTracking()
                .SelectMany(x => x.EntityChanges
                    .Where(y => !startTime.HasValue || y.ChangeTime >= startTime.Value)
                    .Where(y => !endTime.HasValue || y.ChangeTime <= endTime.Value)
                    .Select(y => y.EntityTypeFullName))
                .Where(x => x != null && x != string.Empty)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync(GetCancellationToken(cancellationToken));
        }

        public virtual async Task<int> DeleteOlderThanAsync(
            DateTime cutoffDate,
            CancellationToken cancellationToken = default)
        {
            var dbSet = await GetDbSetAsync();
            return await dbSet
                .Where(x => x.ExecutionTime < cutoffDate)
                .ExecuteDeleteAsync(GetCancellationToken(cancellationToken));
        }

        private async Task<string> ResolveUsername(Guid userId)
        {
            if(userId == BackgroundJobConstants.BackgroundJobPersonId)
            {
                return $"{BackgroundJobConstants.BackgroundJobName}";
            }

            var user = await identityUserRepository.GetAsync(userId);            
            return user != null ? $"{user.Name} {user.Surname}" : string.Empty;
        }
    }
}
