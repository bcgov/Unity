using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Unity.Modules.Shared.Permissions;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.MultiTenancy;

namespace Unity.Notifications.Logs;

[Authorize(IdentityConsts.ITOperationsPermissionName)]
public class NotificationLogsReadAppService(
    INotificationLogsRepository notificationLogsRepository,
    IIdentityUserRepository identityUserRepository,
    ICurrentTenant currentTenant)
    : NotificationsAppService, INotificationLogsReadAppService
{
    public async Task<PagedResultDto<NotificationLogListDto>> GetListAsync(GetNotificationLogsInput input)
    {
        var query = await notificationLogsRepository.GetQueryableAsync();

        query = query.WhereIf(currentTenant.Id.HasValue, x => x.TenantId == currentTenant.Id);

        if (!currentTenant.Id.HasValue && input.TenantId.HasValue)
        {
            query = query.Where(x => x.TenantId == input.TenantId);
        }

        if (input.UserId.HasValue)
        {
            query = query.Where(x => x.UserId == input.UserId);
        }

        if (input.NotificationType.HasValue)
        {
            query = query.Where(x => x.NotificationType == input.NotificationType);
        }

        if (input.Severity.HasValue)
        {
            query = query.Where(x => x.Severity == input.Severity);
        }

        if (input.Channel.HasValue)
        {
            query = query.Where(x => x.Channel == input.Channel);
        }

        if (input.DateFrom.HasValue)
        {
            query = query.Where(x => x.CreationTime >= input.DateFrom.Value);
        }

        if (input.DateTo.HasValue)
        {
            var toDateExclusive = input.DateTo.Value.Date.AddDays(1);
            query = query.Where(x => x.CreationTime < toDateExclusive);
        }

        if (!string.IsNullOrWhiteSpace(input.SearchText))
        {
            var term = input.SearchText.Trim();
            query = query.Where(x =>
                x.Title.Contains(term) ||
                x.Message.Contains(term) ||
                x.Source.Contains(term) ||
                (x.CorrelationId != null && x.CorrelationId.Contains(term)));
        }

        var totalCount = await AsyncExecuter.CountAsync(query);

        var pageQuery = query
            .OrderByDescending(x => x.CreationTime)
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount)
            .Select(x => new NotificationLogListDto
            {
                Id = x.Id,
                CreationTime = x.CreationTime,
                TenantId = x.TenantId,
                UserId = x.UserId,
                NotificationType = x.NotificationType,
                Severity = x.Severity,
                Channel = x.Channel,
                Title = x.Title,
                Message = x.Message,
                Source = x.Source,
                CorrelationId = x.CorrelationId,
                IsDeliveredRealtime = x.IsDeliveredRealtime
            });

        var items = await AsyncExecuter.ToListAsync(pageQuery);

        var userDisplayNames = await GetUserDisplayNamesAsync(
            items.Where(x => x.UserId.HasValue).Select(x => x.UserId!.Value).Distinct());

        foreach (var item in items)
        {
            if (item.UserId.HasValue && userDisplayNames.TryGetValue(item.UserId.Value, out var displayName))
            {
                item.UserDisplayName = displayName;
            }
        }

        return new PagedResultDto<NotificationLogListDto>(totalCount, items);
    }

    private async Task<Dictionary<Guid, string>> GetUserDisplayNamesAsync(IEnumerable<Guid> userIds)
    {
        var displayNames = new Dictionary<Guid, string>();

        foreach (var userId in userIds)
        {
            var user = await identityUserRepository.FindAsync(userId);

            if (user != null)
            {
                displayNames[userId] = $"{user.Name} {user.Surname}".Trim();
            }
        }

        return displayNames;
    }

    public async Task<NotificationLogDetailDto> GetAsync(Guid id)
    {
        var query = await notificationLogsRepository.GetQueryableAsync();

        query = query.WhereIf(currentTenant.Id.HasValue, x => x.TenantId == currentTenant.Id);

        var entity = await AsyncExecuter.FirstOrDefaultAsync(query.Where(x => x.Id == id));

        if (entity == null)
        {
            throw new EntityNotFoundException(typeof(NotificationLog), id);
        }

        return new NotificationLogDetailDto
        {
            Id = entity.Id,
            CreationTime = entity.CreationTime,
            TenantId = entity.TenantId,
            UserId = entity.UserId,
            NotificationType = entity.NotificationType,
            Severity = entity.Severity,
            Channel = entity.Channel,
            Title = entity.Title,
            Message = entity.Message,
            Source = entity.Source,
            SourceReference = entity.SourceReference,
            CorrelationId = entity.CorrelationId,
            IsDeliveredRealtime = entity.IsDeliveredRealtime,
            DeliveryTarget = entity.DeliveryTarget,
            ExceptionType = entity.ExceptionType,
            ExceptionMessage = entity.ExceptionMessage,
            StackExcerpt = entity.StackExcerpt,
            CommitSha = entity.CommitSha,
            Environment = entity.Environment,
            PayloadJson = entity.PayloadJson
        };
    }
}
