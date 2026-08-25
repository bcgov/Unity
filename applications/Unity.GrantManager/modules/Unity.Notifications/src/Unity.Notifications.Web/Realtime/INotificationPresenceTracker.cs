using System;
using System.Collections.Generic;

namespace Unity.Notifications.Web.Realtime;

public interface INotificationPresenceTracker
{
    void UserConnected(string connectionId, string userId, string userName, Guid? tenantId, bool isItOperations);

    void UserDisconnected(string connectionId);

    void Heartbeat(string connectionId);

    IReadOnlyList<OnlineUserInfo> GetOnlineUsers();
}
