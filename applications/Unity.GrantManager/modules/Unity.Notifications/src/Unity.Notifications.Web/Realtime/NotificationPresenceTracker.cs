using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Unity.Notifications.Web.Realtime;

public class NotificationPresenceTracker : INotificationPresenceTracker
{
    private readonly ConcurrentDictionary<string, string> connectionToUser = new();

    private readonly ConcurrentDictionary<string, PresenceRecord> users = new();

    public void UserConnected(string connectionId, string userId, string userName, Guid? tenantId, bool isItOperations)
    {
        var presenceKey = BuildPresenceKey(userId, tenantId);
        connectionToUser[connectionId] = presenceKey;

        var record = users.GetOrAdd(presenceKey, _ => new PresenceRecord
        {
            UserId = userId,
            UserName = userName,
            TenantId = tenantId,
            IsItOperations = isItOperations
        });

        record.UserName = string.IsNullOrWhiteSpace(userName) ? record.UserName : userName;
        record.TenantId = tenantId;
        record.IsItOperations = isItOperations;
        record.LastActivityUtc = DateTime.UtcNow;
        record.Connections[connectionId] = 0;
    }

    public void UserDisconnected(string connectionId)
    {
        if (!connectionToUser.TryRemove(connectionId, out var presenceKey))
        {
            return;
        }

        if (!users.TryGetValue(presenceKey, out var record))
        {
            return;
        }

        record.Connections.TryRemove(connectionId, out _);

        if (record.Connections.IsEmpty)
        {
            users.TryRemove(presenceKey, out _);
        }
    }

    public void Heartbeat(string connectionId)
    {
        if (!connectionToUser.TryGetValue(connectionId, out var presenceKey))
        {
            return;
        }

        if (users.TryGetValue(presenceKey, out var record))
        {
            record.LastActivityUtc = DateTime.UtcNow;
        }
    }

    public IReadOnlyList<OnlineUserInfo> GetOnlineUsers()
    {
        return users.Values
            .Select(record => new OnlineUserInfo
            {
                UserId = record.UserId,
                UserName = record.UserName,
                TenantId = record.TenantId,
                IsItOperations = record.IsItOperations,
                ConnectionCount = record.Connections.Count,
                LastActivityUtc = record.LastActivityUtc
            })
            .OrderBy(x => x.UserName)
            .ThenBy(x => x.UserId)
            .ToList();
    }

    private static string BuildPresenceKey(string userId, Guid? tenantId)
    {
        return $"{tenantId?.ToString() ?? "host"}:{userId}";
    }

    private class PresenceRecord
    {
        public string UserId { get; set; } = string.Empty;

        public string UserName { get; set; } = string.Empty;

        public Guid? TenantId { get; set; }

        public bool IsItOperations { get; set; }

        public DateTime LastActivityUtc { get; set; }

        public ConcurrentDictionary<string, byte> Connections { get; } = new();
    }
}
