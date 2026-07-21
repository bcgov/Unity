using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.AuditLogging;
using Volo.Abp.Data;
using Volo.Abp.Domain.ChangeTracking;
using Volo.Abp.Identity;

namespace Unity.GrantManager.History;

public class HistoryAppService(
    IAuditLogRepository auditLogRepository,
    IIdentityUserRepository identityUserRepository,
    IDataFilter softDataFilter) : GrantManagerAppService, IHistoryAppService
{
    /// <summary>
    /// Gets the list of entity property changes based on the provided input parameters.
    /// </summary>
    /// <param name="entityId">The ID of the entity for which to retrieve property changes.</param>
    /// <param name="filterPropertyName">The name of the property to filter changes by.</param>
    /// <param name="lookupDictionary">An optional dictionary for looking up display values for property changes.</param>
    /// <returns>A list of history DTOs representing the entity property changes.</returns>
    [DisableEntityChangeTracking]
    public virtual async Task<List<HistoryDto>> GetHistoryList(
        string? entityId,
        string filterPropertyName,
        Dictionary<string, string>? lookupDictionary)
    {
        return await GetEntityPropertyChangesAsync(
            new GetEntityPropertyChangesInput
            {
                EntityId = entityId,
                PropertyNames = [filterPropertyName],
            },
            lookupDictionary);
    }

    /// <summary>
    /// Gets the list of entity property changes for a specific entity.
    /// </summary>
    /// <param name="input">The input parameters for retrieving entity property changes.</param>
    /// <param name="lookupDictionary">An optional dictionary for looking up display values for property changes.</param>
    /// <returns>A list of history DTOs representing the entity property changes.</returns>
    [DisableEntityChangeTracking]
    public virtual async Task<List<HistoryDto>> GetEntityPropertyChangesAsync(
        GetEntityPropertyChangesInput input,
        Dictionary<string, string>? lookupDictionary = null)
    {
        var entityChanges = await auditLogRepository.GetEntityChangeListAsync(
            sorting: null,
            maxResultCount: input.MaxResultCount,
            skipCount: input.SkipCount,
            auditLogId: null,
            startTime: input.StartTime,
            endTime: input.EndTime,
            changeType: null,
            entityId: input.EntityId,
            entityTypeFullName: input.EntityTypeFullName,
            includeDetails: true,
            cancellationToken: default);

        var propertyNames = (input.PropertyNames ?? []).ToHashSet(StringComparer.Ordinal);
        var historyList = new List<HistoryDto>();
        var userNameCache = new Dictionary<Guid, string>();

        foreach (var entityChange in entityChanges)
        {
            foreach (var propertyChange in entityChange.PropertyChanges)
            {
                if (propertyNames.Count > 0 && !propertyNames.Contains(propertyChange.PropertyName))
                {
                    continue;
                }

                string originalValue = CleanValue(propertyChange.OriginalValue);
                string newValue = CleanValue(propertyChange.NewValue);
                DateTime utcDateTime = DateTime.SpecifyKind(entityChange.ChangeTime, DateTimeKind.Utc);

                if (!userNameCache.TryGetValue(entityChange.AuditLogId, out var userName))
                {
                    userName = await LookupUserNameAsync(entityChange.AuditLogId);
                    userNameCache[entityChange.AuditLogId] = userName;
                }

                historyList.Add(new HistoryDto
                {
                    PropertyName = propertyChange.PropertyName,
                    PropertyTypeFullName = propertyChange.PropertyTypeFullName ?? string.Empty,
                    OriginalValue = GetLookupValue(originalValue, lookupDictionary),
                    NewValue = GetLookupValue(newValue, lookupDictionary),
                    ChangeTime = utcDateTime,
                    ChangeType = (int)entityChange.ChangeType,
                    UserName = userName,
                });
            }
        }

        return historyList;
    }

    public virtual async Task<string> LookupUserNameAsync(Guid auditLogId)
    {
        var auditLog = await auditLogRepository.GetAsync(auditLogId);
        if (auditLog?.UserId == null || auditLog.UserId == Guid.Empty)
        {
            return string.Empty;
        }

        var userId = auditLog.UserId.Value;
        using (softDataFilter.Disable<ISoftDelete>())
        {
            var user = await identityUserRepository.FindAsync(userId);
            return user != null ? $"{user.Name} {user.Surname}" : "(Full-Deleted User)";
        }
    }

    public async Task<string> LookupUserName(Guid auditLogId)
    {
        return await LookupUserNameAsync(auditLogId);
    }

    private static string CleanValue(string? value)
    {
        return value?.Replace("\"", "") ?? "";
    }

    private static string GetLookupValue(string value, Dictionary<string, string>? lookupDictionary)
    {
        return lookupDictionary != null && lookupDictionary.TryGetValue(value, out var lookupValue)
            ? lookupValue
            : value;
    }
}
