using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Unity.Modules.Shared.Permissions;
using Unity.Notifications.Logs;
using Unity.Notifications.Features;
using Unity.Notifications.ReadStates;
using Volo.Abp.AspNetCore.SignalR;
using Volo.Abp.Features;
using Volo.Abp.Identity;
using Volo.Abp.MultiTenancy;

namespace Unity.Notifications.Web.Realtime;

[Authorize]
[HubRoute(HubRoute)]
public class NotificationHub(
    ICurrentTenant currentTenant,
    INotificationPresenceTracker presenceTracker,
    INotificationLogsAppService notificationLogsAppService,
    IIdentityUserRepository identityUserRepository,
    IFeatureChecker featureChecker,
    INotificationLogsRepository notificationLogsRepository,
    NotificationReadStateManager notificationReadStateManager) : Hub
{
    public const string HubRoute = "/signalr/notifications";
    public const string NotificationLogsOpsGroup = "ops:notification-logs";
    public const int MaxDirectMessageLength = 4000;

    public override async Task OnConnectedAsync()
    {
        if (!await featureChecker.IsEnabledAsync(NotificationsFeatureConsts.DirectMessaging))
        {
            Context.Abort();
            return;
        }

        var userId = GetCurrentUserId();
        var isItOperations = IsItOperationsUser();

        if (!string.IsNullOrWhiteSpace(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, BuildUserGroup(userId));

            var userName = DisplayNameHelper.Resolve(Context.User);

            presenceTracker.UserConnected(
                Context.ConnectionId,
                userId,
                userName,
                currentTenant.Id,
                isItOperations);
        }

        if (currentTenant.Id.HasValue)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, BuildTenantGroup(currentTenant.Id.Value));
        }

        if (isItOperations)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, NotificationLogsOpsGroup);
        }

        await BroadcastOnlineUsersAsync();
        await BroadcastTenantPresenceAsync();

        await base.OnConnectedAsync();
    }

    public async Task<UnreadMessageInfo[]> GetUnreadMessagesAsync()
    {
        if (!Guid.TryParse(GetCurrentUserId(), out var userId))
        {
            return [];
        }

        var lastReadAt = await notificationReadStateManager.GetLastReadAtAsync(userId, currentTenant.Id);
        var query = await notificationLogsRepository.GetQueryableAsync();
        var messages = await query
            .Where(x => x.NotificationType == NotificationLogType.SignalRDirectMessage
                && x.CreationTime > lastReadAt
                && (x.UserId == userId || (x.UserId == null && x.TenantId == currentTenant.Id)))
            .OrderBy(x => x.CreationTime)
            .Take(50)
            .ToListAsync();

        return [.. messages.Select(message => new UnreadMessageInfo
        {
            Scope = message.UserId.HasValue ? "user" : "tenant",
            TargetId = message.UserId.HasValue
                ? message.SenderUserId?.ToString()
                : message.TenantId?.ToString(),
            SenderId = message.SenderUserId?.ToString(),
            SenderName = message.SenderDisplayName ?? message.SenderUserId?.ToString() ?? "unknown",
            Source = message.Source,
            Message = message.Message,
            Timestamp = message.CreationTime
        })];
    }

    public async Task<UnreadMessageInfo[]> GetConversationHistoryAsync(string scope, string? targetId)
    {
        if (!Guid.TryParse(GetCurrentUserId(), out var userId))
        {
            return [];
        }

        var query = await notificationLogsRepository.GetQueryableAsync();
        var messageQuery = query.Where(x =>
            x.NotificationType == NotificationLogType.SignalRDirectMessage
            && x.TenantId == currentTenant.Id);

        if (scope == "tenant")
        {
            messageQuery = messageQuery.Where(x => x.UserId == null);
        }
        else if (Guid.TryParse(targetId, out var peerUserId))
        {
            messageQuery = messageQuery.Where(x =>
                (x.UserId == userId && x.SenderUserId == peerUserId)
                || (x.UserId == peerUserId && x.SenderUserId == userId));
        }
        else
        {
            return [];
        }

        var messages = await messageQuery
            .OrderByDescending(x => x.CreationTime)
            .Take(100)
            .ToListAsync();

        return [.. messages
            .OrderBy(x => x.CreationTime)
            .Select(message => new UnreadMessageInfo
            {
                Scope = message.UserId.HasValue ? "user" : "tenant",
                TargetId = message.UserId.HasValue
                    ? (message.SenderUserId == userId ? message.UserId : message.SenderUserId)?.ToString()
                    : message.TenantId?.ToString(),
                SenderId = message.SenderUserId?.ToString(),
                SenderName = message.SenderDisplayName ?? message.SenderUserId?.ToString() ?? "unknown",
                Source = message.Source,
                Message = message.Message,
                Timestamp = message.CreationTime
            })];
    }

    public Task MarkMessagesReadAsync()
    {
        return Guid.TryParse(GetCurrentUserId(), out var userId)
            ? notificationReadStateManager.MarkReadAsync(userId, currentTenant.Id)
            : Task.CompletedTask;
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        presenceTracker.UserDisconnected(Context.ConnectionId);
        await BroadcastOnlineUsersAsync();
        await BroadcastTenantPresenceAsync();

        await base.OnDisconnectedAsync(exception);
    }

    public async Task HeartbeatAsync()
    {
        presenceTracker.Heartbeat(Context.ConnectionId);

        await BroadcastTenantPresenceAsync();
    }

    public Task<OnlineUserInfo[]> GetOnlineUsersAsync()
    {
        if (!IsItOperationsUser())
        {
            throw new HubException("Only ITOperations users can access online presence.");
        }

        return Task.FromResult(presenceTracker.GetOnlineUsers().ToArray());
    }

    public async Task SendDirectMessageAsync(string targetUserId, string message)
    {
        if (!IsItOperationsUser())
        {
            throw new HubException("Only ITOperations users can send direct messages.");
        }

        if (string.IsNullOrWhiteSpace(targetUserId) || string.IsNullOrWhiteSpace(message))
        {
            throw new HubException("Target user and message are required.");
        }

        if (message.Length > MaxDirectMessageLength)
        {
            throw new HubException($"Messages cannot exceed {MaxDirectMessageLength} characters.");
        }

        var senderId = GetCurrentUserId() ?? string.Empty;
        var senderName = DisplayNameHelper.Resolve(Context.User);
        var timestamp = DateTime.UtcNow;

        await Clients.Group(BuildUserGroup(targetUserId)).SendAsync("directMessageReceived", new
        {
            scope = "user",
            source = nameof(NotificationHub),
            senderId,
            senderName,
            message,
            timestamp
        });

        await notificationLogsAppService.CreateAsync(new CreateNotificationLogDto
        {
            TenantId = currentTenant.Id,
            UserId = Guid.TryParse(targetUserId, out var parsedTargetUserId) ? parsedTargetUserId : null,
            SenderUserId = Guid.TryParse(senderId, out var parsedSenderUserId) ? parsedSenderUserId : null,
            SenderDisplayName = senderName,
            NotificationType = NotificationLogType.SignalRDirectMessage,
            Channel = NotificationLogChannel.SignalR,
            Severity = NotificationLogSeverity.Info,
            Title = "Direct message sent",
            Message = message,
            Source = nameof(NotificationHub),
            CorrelationId = Context.ConnectionId,
            IsDeliveredRealtime = true,
            DeliveryTarget = BuildUserGroup(targetUserId),
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
        });
    }

    public async Task<TenantUserPresenceInfo[]> GetTenantUsersAsync()
    {
        return await BuildTenantPresenceListAsync();
    }

    public Task<object?> GetCurrentTenantAsync()
    {
        return Task.FromResult(currentTenant.Id.HasValue
            ? (object)new { id = currentTenant.Id, name = currentTenant.Name }
            : null);
    }

    public async Task SendTenantMessageAsync(string message)
    {
        if (!currentTenant.Id.HasValue || string.IsNullOrWhiteSpace(message))
        {
            throw new HubException("A tenant and message are required.");
        }

        var senderId = GetCurrentUserId() ?? string.Empty;
        var senderName = DisplayNameHelper.Resolve(Context.User);
        var timestamp = DateTime.UtcNow;

        await Clients.Group(BuildTenantGroup(currentTenant.Id.Value)).SendAsync("directMessageReceived", new
        {
            scope = "tenant",
            source = nameof(NotificationHub),
            tenantId = currentTenant.Id,
            senderId,
            senderName,
            message,
            timestamp
        });

        await notificationLogsAppService.CreateAsync(new CreateNotificationLogDto
        {
            TenantId = currentTenant.Id,
            SenderUserId = Guid.TryParse(senderId, out var parsedSenderUserId) ? parsedSenderUserId : null,
            SenderDisplayName = senderName,
            NotificationType = NotificationLogType.SignalRDirectMessage,
            Channel = NotificationLogChannel.SignalR,
            Severity = NotificationLogSeverity.Info,
            Title = "Tenant broadcast sent",
            Message = message,
            Source = nameof(NotificationHub),
            CorrelationId = Context.ConnectionId,
            IsDeliveredRealtime = true,
            DeliveryTarget = BuildTenantGroup(currentTenant.Id.Value),
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
        });
    }

    public async Task SendPeerMessageAsync(string targetUserId, string message)
    {
        if (string.IsNullOrWhiteSpace(targetUserId) || string.IsNullOrWhiteSpace(message))
        {
            throw new HubException("Target user and message are required.");
        }

        if (!Guid.TryParse(targetUserId, out var targetUserGuid))
        {
            throw new HubException("Target user is invalid.");
        }

        // Tenant membership is enforced by ABP's automatic multi-tenancy filter on the repository query,
        // so this returns null for users outside the current tenant even if the id is otherwise valid.
        var targetUser = await identityUserRepository.FindAsync(targetUserGuid) ?? throw new HubException("Target user was not found in your tenant.");
        var senderId = GetCurrentUserId() ?? string.Empty;
        var senderName = DisplayNameHelper.Resolve(Context.User);
        var timestamp = DateTime.UtcNow;

        await Clients.Group(BuildUserGroup(targetUserId)).SendAsync("directMessageReceived", new
        {
            scope = "user",
            source = nameof(NotificationHub),
            senderId,
            senderName,
            message,
            timestamp
        });

        await notificationLogsAppService.CreateAsync(new CreateNotificationLogDto
        {
            TenantId = currentTenant.Id,
            UserId = Guid.TryParse(targetUserId, out var parsedTargetUserId) ? parsedTargetUserId : null,
            SenderUserId = Guid.TryParse(senderId, out var parsedSenderUserId) ? parsedSenderUserId : null,
            SenderDisplayName = senderName,
            NotificationType = NotificationLogType.SignalRDirectMessage,
            Channel = NotificationLogChannel.SignalR,
            Severity = NotificationLogSeverity.Info,
            Title = "Peer message sent",
            Message = message,
            Source = nameof(NotificationHub),
            CorrelationId = Context.ConnectionId,
            IsDeliveredRealtime = true,
            DeliveryTarget = BuildUserGroup(targetUserId),
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
        });
    }

    public static string BuildUserGroup(string userId) => $"user:{userId}";

    public static string BuildTenantGroup(Guid tenantId) => $"tenant:{tenantId}";

    public sealed class UnreadMessageInfo
    {
        public string Scope { get; set; } = string.Empty;
        public string? TargetId { get; set; }
        public string? SenderId { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    private Task BroadcastOnlineUsersAsync()
    {
        return Clients.Group(NotificationLogsOpsGroup)
            .SendAsync("onlineUsersUpdated", presenceTracker.GetOnlineUsers());
    }

    private async Task BroadcastTenantPresenceAsync()
    {
        if (!currentTenant.Id.HasValue)
        {
            return;
        }

        var presenceList = await BuildTenantPresenceListAsync();

        await Clients.Group(BuildTenantGroup(currentTenant.Id.Value))
            .SendAsync("tenantPresenceUpdated", presenceList);
    }

    private async Task<TenantUserPresenceInfo[]> BuildTenantPresenceListAsync()
    {
        var tenantUsers = await identityUserRepository.GetListAsync(maxResultCount: int.MaxValue);

        var onlineByUserId = presenceTracker.GetOnlineUsers()
            .Where(u => u.TenantId == currentTenant.Id)
            .ToDictionary(u => u.UserId);

        return [.. tenantUsers
            .Select(user =>
            {
                var userIdString = user.Id.ToString();
                var isOnline = onlineByUserId.TryGetValue(userIdString, out var online);

                return new TenantUserPresenceInfo
                {
                    UserId = userIdString,
                    UserName = DisplayNameHelper.Resolve(user),
                    IsOnline = isOnline,
                    ConnectionCount = isOnline ? online!.ConnectionCount : 0,
                    LastActivityUtc = isOnline ? online!.LastActivityUtc : null
                };
            })
            .OrderByDescending(x => x.IsOnline)
            .ThenBy(x => x.UserName)];
    }

    private string? GetCurrentUserId()
    {
        return Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? Context.User?.FindFirstValue("sub")
            ?? Context.User?.FindFirstValue("userid")
            ?? Context.User?.FindFirstValue("user_id");
    }

    private bool IsItOperationsUser()
    {
        if (Context.User?.IsInRole(IdentityConsts.ITOperationsRoleName) == true)
        {
            return true;
        }

        var user = Context.User;

        if (user == null)
        {
            return false;
        }

        var roleClaimTypes = new[] { ClaimTypes.Role, "role", "roles" };

        foreach (var type in roleClaimTypes)
        {
            if (user.Claims.Any(c =>
                    string.Equals(c.Type, type, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(c.Value, IdentityConsts.ITOperationsRoleName, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
        }

        var realmAccessClaim = user.Claims.FirstOrDefault(c =>
            string.Equals(c.Type, "realm_access", StringComparison.OrdinalIgnoreCase));

        if (realmAccessClaim == null || string.IsNullOrWhiteSpace(realmAccessClaim.Value))
        {
            return false;
        }

        try
        {
            using var json = JsonDocument.Parse(realmAccessClaim.Value);

            if (json.RootElement.TryGetProperty("roles", out var rolesElement)
                && rolesElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var role in rolesElement.EnumerateArray())
                {
                    if (role.ValueKind == JsonValueKind.String
                        && string.Equals(role.GetString(), IdentityConsts.ITOperationsRoleName, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
        }
        catch
        {
            return false;
        }

        return false;
    }
}
