using System;

namespace Unity.Notifications.Web.Realtime;

public class OnlineUserInfo
{
    public string UserId { get; set; } = string.Empty;

    public string UserName { get; set; } = string.Empty;

    public Guid? TenantId { get; set; }

    public bool IsItOperations { get; set; }

    public int ConnectionCount { get; set; }

    public DateTime LastActivityUtc { get; set; }
}
