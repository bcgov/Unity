using System;

namespace Unity.Notifications.Web.Realtime;

public class TenantUserPresenceInfo
{
    public string UserId { get; set; } = string.Empty;

    public string UserName { get; set; } = string.Empty;

    public bool IsOnline { get; set; }

    public int ConnectionCount { get; set; }

    public DateTime? LastActivityUtc { get; set; }
}
