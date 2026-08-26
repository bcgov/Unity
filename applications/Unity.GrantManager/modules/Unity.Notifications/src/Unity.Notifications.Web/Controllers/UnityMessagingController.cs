using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
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
using Unity.GrantManager.ApplicationForms;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AuditLogging;
using Volo.Abp.Identity;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Features;
using Volo.Abp.TenantManagement;
using Volo.Abp.Security.Encryption;
using Unity.GrantManager.Tokens;

namespace Unity.Notifications.Web.Controllers;

[RemoteService(true)]
[Authorize(IdentityConsts.ITOperationsPermissionName)]
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
    private readonly ITenantTokenRepository tenantTokenRepository;
    private readonly IStringEncryptionService stringEncryptionService;
    private readonly IFeatureChecker featureChecker;

    public UnityMessagingController(
        IHubContext<NotificationHub> hubContext,
        INotificationPresenceTracker presenceTracker,
        INotificationLogsAppService notificationLogsAppService,
        IIdentityUserRepository identityUserRepository,
        ITenantRepository tenantRepository,
        IAuditLogRepository auditLogRepository,
        ICurrentTenant currentTenant,
        ITenantTokenRepository tenantTokenRepository,
        IStringEncryptionService stringEncryptionService,
        IFeatureChecker featureChecker)
    {
        this.hubContext = hubContext;
        this.presenceTracker = presenceTracker;
        this.notificationLogsAppService = notificationLogsAppService;
        this.identityUserRepository = identityUserRepository;
        this.tenantRepository = tenantRepository;
        this.auditLogRepository = auditLogRepository;
        this.currentTenant = currentTenant;
        this.tenantTokenRepository = tenantTokenRepository;
        this.stringEncryptionService = stringEncryptionService;
        this.featureChecker = featureChecker;
        LocalizationResource = typeof(NotificationsResource);
    }

    [RequiresFeature(NotificationsFeatureConsts.DirectMessaging)]
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

    [RequiresFeature(NotificationsFeatureConsts.DirectMessaging)]
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

        var senderUserId = CurrentUser.Id?.ToString();
        var senderName = CurrentUser.Id.HasValue ? DisplayNameHelper.Resolve(CurrentUser) : string.Empty;

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

    [RequiresFeature(NotificationsFeatureConsts.DirectMessaging)]
    [HttpPost("message-tenant")]
    public async Task<IActionResult> MessageTenantAsync([FromBody] TenantMessageRequest request)
    {
        return await SendTenantMessageAsync(request);
    }

    [AllowAnonymous]
    [HttpPost("message-tenant-api")]
    public async Task<IActionResult> MessageTenantWithApiKeyAsync([FromBody] TenantMessageRequest request)
    {
        if (!await IsTenantApiKeyValidAsync(request.TargetTenantId))
        {
            return Unauthorized("Invalid API key for the target tenant.");
        }

        using (currentTenant.Change(request.TargetTenantId))
        {
            if (!await featureChecker.IsEnabledAsync(NotificationsFeatureConsts.DirectMessaging))
            {
                return Forbid();
            }
        }

        return await SendTenantMessageAsync(request);
    }

    private async Task<IActionResult> SendTenantMessageAsync(TenantMessageRequest request)
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

        if (await tenantRepository.FindAsync(request.TargetTenantId) is null)
        {
            return NotFound("Target tenant was not found.");
        }

        var senderUserId = CurrentUser.Id?.ToString();
        var senderName = CurrentUser.Id.HasValue ? DisplayNameHelper.Resolve(CurrentUser) : string.Empty;
        var normalizedMessageType = request.MessageType.ToLowerInvariant();
        var timestamp = DateTime.UtcNow;

        await hubContext.Clients.Group(NotificationHub.BuildTenantGroup(request.TargetTenantId)).SendAsync(
            "directMessageReceived",
            new
            {
                scope = "tenant",
                source = nameof(UnityMessagingController),
                tenantId = request.TargetTenantId,
                senderId = senderUserId,
                senderName,
                message = request.Message,
                messageType = normalizedMessageType,
                timestamp
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
            PayloadJson = JsonSerializer.Serialize(new { messageType = normalizedMessageType }),
            Source = nameof(UnityMessagingController),
            CorrelationId = HttpContext.TraceIdentifier,
            IsDeliveredRealtime = true,
            DeliveryTarget = NotificationHub.BuildTenantGroup(request.TargetTenantId),
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
        });

        return Ok(new { delivered = true, logId });
    }

    private async Task<bool> IsTenantApiKeyValidAsync(Guid tenantId)
    {
        if (tenantId == Guid.Empty
            || !HttpContext.Request.Headers.TryGetValue(AuthConstants.ApiKeyHeader, out var extractedApiKey))
        {
            return false;
        }

        var query = await tenantTokenRepository.GetQueryableAsync();
        var tenantToken = query.FirstOrDefault(token =>
            token.TenantId == tenantId && token.Name == TokenConsts.IntakeApiName);

        if (string.IsNullOrWhiteSpace(tenantToken?.Value))
        {
            return false;
        }

        var expectedApiKey = stringEncryptionService.Decrypt(tenantToken.Value) ?? string.Empty;
        var actualApiKey = extractedApiKey.ToString();
        var expectedBytes = Encoding.UTF8.GetBytes(expectedApiKey);
        var actualBytes = Encoding.UTF8.GetBytes(actualApiKey);

        return CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
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
