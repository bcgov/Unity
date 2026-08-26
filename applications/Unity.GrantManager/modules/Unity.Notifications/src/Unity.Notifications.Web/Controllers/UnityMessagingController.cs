using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Unity.Modules.Shared.Permissions;
using Unity.Notifications.Localization;
using Unity.Notifications.Logs;
using Unity.Notifications.Features;
using Unity.Notifications.Web.Realtime;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AuditLogging;
using Volo.Abp.Identity;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Features;
using Volo.Abp.TenantManagement;

namespace Unity.Notifications.Web.Controllers;

[RemoteService(true)]
[Authorize(IdentityConsts.ITOperationsPermissionName)]
[RequiresFeature(NotificationsFeatureConsts.DirectMessaging)]
[Route("api/notifications/unity-messaging")]
public class UnityMessagingController : AbpControllerBase
{
    private readonly IHubContext<NotificationHub> hubContext;
    private readonly INotificationPresenceTracker presenceTracker;
    private readonly INotificationLogsAppService notificationLogsAppService;
    private readonly IIdentityUserRepository identityUserRepository;
    private readonly ITenantRepository tenantRepository;
    private readonly IAuditLogRepository auditLogRepository;
    private readonly ICurrentTenant currentTenant;

    public UnityMessagingController(
        IHubContext<NotificationHub> hubContext,
        INotificationPresenceTracker presenceTracker,
        INotificationLogsAppService notificationLogsAppService,
        IIdentityUserRepository identityUserRepository,
        ITenantRepository tenantRepository,
        IAuditLogRepository auditLogRepository,
        ICurrentTenant currentTenant)
    {
        this.hubContext = hubContext;
        this.presenceTracker = presenceTracker;
        this.notificationLogsAppService = notificationLogsAppService;
        this.identityUserRepository = identityUserRepository;
        this.tenantRepository = tenantRepository;
        this.auditLogRepository = auditLogRepository;
        this.currentTenant = currentTenant;
        LocalizationResource = typeof(NotificationsResource);
    }

    [HttpGet("online-users")]
    public async Task<ActionResult<IReadOnlyList<OnlineUserInfo>>> GetOnlineUsersAsync()
    {
        var tenantIds = currentTenant.Id.HasValue
            ? [currentTenant.Id.Value]
            : (await tenantRepository.GetListAsync()).Select(tenant => tenant.Id).ToArray();
        var result = new List<OnlineUserInfo>();

        foreach (var tenantId in tenantIds)
        {
            using (currentTenant.Change(tenantId))
            {
                var users = await identityUserRepository.GetListAsync(maxResultCount: int.MaxValue);
                var onlineUsersById = presenceTracker.GetOnlineUsers()
                    .Where(user => user.TenantId == tenantId)
                    .ToDictionary(user => user.UserId);
                var auditQuery = await auditLogRepository.GetQueryableAsync();
                var lastActivityByUserId = await auditQuery
                    .Where(log => log.TenantId == tenantId && log.UserId.HasValue)
                    .GroupBy(log => log.UserId!.Value)
                    .Select(group => new
                    {
                        UserId = group.Key,
                        LastActivityUtc = group.Max(log => log.ExecutionTime)
                    })
                    .ToDictionaryAsync(activity => activity.UserId);

                result.AddRange(users.Select(user =>
                {
                    var userId = user.Id.ToString();
                    var isOnline = onlineUsersById.TryGetValue(userId, out var onlineUser);
                    lastActivityByUserId.TryGetValue(user.Id, out var auditActivity);

                    return new OnlineUserInfo
                    {
                        UserId = userId,
                        UserName = DisplayNameHelper.Resolve(user),
                        TenantId = tenantId,
                        IsItOperations = isOnline && onlineUser!.IsItOperations,
                        ConnectionCount = isOnline ? onlineUser!.ConnectionCount : 0,
                        LastActivityUtc = auditActivity?.LastActivityUtc
                    };
                }));
            }
        }

        result = result
            .OrderByDescending(user => user.IsOnline)
            .ThenByDescending(user => user.LastActivityUtc)
            .ThenBy(user => user.UserName)
            .ToList();

        return Ok(result);
    }

    [HttpPost("message-user")]
    public async Task<IActionResult> MessageUserAsync([FromBody] DirectMessageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TargetUserId) || string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest("TargetUserId and Message are required.");
        }

        if (request.Message.Length > NotificationHub.MaxDirectMessageLength)
        {
            return BadRequest($"Messages cannot exceed {NotificationHub.MaxDirectMessageLength} characters.");
        }

        var senderUserId = CurrentUser.Id?.ToString() ?? "unknown";
        var senderName = DisplayNameHelper.Resolve(CurrentUser);

        await hubContext.Clients.Group(NotificationHub.BuildUserGroup(request.TargetUserId)).SendAsync(
            "directMessageReceived",
            new
            {
                scope = "user",
                source = nameof(UnityMessagingController),
                senderId = senderUserId,
                senderName,
                message = request.Message,
                timestamp = DateTime.UtcNow
            });

        var logId = await notificationLogsAppService.CreateAsync(new CreateNotificationLogDto
        {
            TenantId = currentTenant.Id,
            UserId = Guid.TryParse(request.TargetUserId, out var targetUserId) ? targetUserId : null,
            SenderUserId = CurrentUser.Id,
            SenderDisplayName = senderName,
            NotificationType = NotificationLogType.SignalRDirectMessage,
            Channel = NotificationLogChannel.SignalR,
            Severity = NotificationLogSeverity.Info,
            Title = "Direct message sent",
            Message = request.Message,
            Source = nameof(UnityMessagingController),
            CorrelationId = HttpContext.TraceIdentifier,
            IsDeliveredRealtime = true,
            DeliveryTarget = NotificationHub.BuildUserGroup(request.TargetUserId),
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
        });

        return Ok(new { delivered = true, logId });
    }

    [HttpPost("message-tenant")]
    public async Task<IActionResult> MessageTenantAsync([FromBody] TenantMessageRequest request)
    {
        if (request.TargetTenantId == Guid.Empty || string.IsNullOrWhiteSpace(request.Message)
            || !IsValidMessageType(request.MessageType))
        {
            return BadRequest("TargetTenantId, Message, and a valid MessageType are required.");
        }

        if (request.Message.Length > NotificationHub.MaxDirectMessageLength)
        {
            return BadRequest($"Messages cannot exceed {NotificationHub.MaxDirectMessageLength} characters.");
        }

        if (currentTenant.Id.HasValue && currentTenant.Id.Value != request.TargetTenantId)
        {
            return Forbid();
        }

        var senderUserId = CurrentUser.Id?.ToString() ?? "unknown";
        var senderName = DisplayNameHelper.Resolve(CurrentUser);

        await hubContext.Clients.Group(NotificationHub.BuildTenantGroup(request.TargetTenantId)).SendAsync(
            "directMessageReceived",
            new
            {
                scope = "tenant",
                source = nameof(UnityMessagingController),
                senderId = senderUserId,
                senderName,
                message = request.Message,
                messageType = request.MessageType.ToLowerInvariant(),
                timestamp = DateTime.UtcNow
            });

        var logId = await notificationLogsAppService.CreateAsync(new CreateNotificationLogDto
        {
            TenantId = request.TargetTenantId,
            SenderUserId = CurrentUser.Id,
            SenderDisplayName = senderName,
            NotificationType = NotificationLogType.SignalRDirectMessage,
            Channel = NotificationLogChannel.SignalR,
            Severity = NotificationLogSeverity.Info,
            Title = "Tenant broadcast sent",
            Message = request.Message,
            PayloadJson = JsonSerializer.Serialize(new { messageType = request.MessageType.ToLowerInvariant() }),
            Source = nameof(UnityMessagingController),
            CorrelationId = HttpContext.TraceIdentifier,
            IsDeliveredRealtime = true,
            DeliveryTarget = NotificationHub.BuildTenantGroup(request.TargetTenantId),
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
        });

        return Ok(new { delivered = true, logId });
    }

    private static bool IsValidMessageType(string? messageType)
    {
        return string.Equals(messageType, "banner", StringComparison.OrdinalIgnoreCase)
            || string.Equals(messageType, "popup", StringComparison.OrdinalIgnoreCase);
    }
}

public class DirectMessageRequest
{
    public string TargetUserId { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
}

public class TenantMessageRequest
{
    public Guid TargetTenantId { get; set; }

    public string Message { get; set; } = string.Empty;

    public string MessageType { get; set; } = string.Empty;
}
