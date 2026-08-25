using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Unity.Modules.Shared.Permissions;
using Unity.Notifications.Localization;
using Unity.Notifications.Logs;
using Unity.Notifications.Features;
using Unity.Notifications.Web.Realtime;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Features;

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
    private readonly ICurrentTenant currentTenant;

    public UnityMessagingController(
        IHubContext<NotificationHub> hubContext,
        INotificationPresenceTracker presenceTracker,
        INotificationLogsAppService notificationLogsAppService,
        ICurrentTenant currentTenant)
    {
        this.hubContext = hubContext;
        this.presenceTracker = presenceTracker;
        this.notificationLogsAppService = notificationLogsAppService;
        this.currentTenant = currentTenant;
        LocalizationResource = typeof(NotificationsResource);
    }

    [HttpGet("online-users")]
    public ActionResult<IReadOnlyList<OnlineUserInfo>> GetOnlineUsers()
    {
        return Ok(presenceTracker.GetOnlineUsers());
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
        if (request.TargetTenantId == Guid.Empty || string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest("TargetTenantId and Message are required.");
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
            Source = nameof(UnityMessagingController),
            CorrelationId = HttpContext.TraceIdentifier,
            IsDeliveredRealtime = true,
            DeliveryTarget = NotificationHub.BuildTenantGroup(request.TargetTenantId),
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
        });

        return Ok(new { delivered = true, logId });
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
}
