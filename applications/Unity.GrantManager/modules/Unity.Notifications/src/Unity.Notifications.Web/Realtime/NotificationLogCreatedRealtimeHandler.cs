using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Unity.Notifications.Logs;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;
using Volo.Abp.EventBus.Local;

namespace Unity.Notifications.Web.Realtime;

public class NotificationLogCreatedRealtimeHandler(IHubContext<NotificationHub> hubContext)
    : ILocalEventHandler<NotificationLogCreatedEto>, ITransientDependency
{
    public async Task HandleEventAsync(NotificationLogCreatedEto eventData)
    {
        await hubContext.Clients.Group(NotificationHub.NotificationLogsOpsGroup)
            .SendAsync("notificationLogCreated", eventData);

        await hubContext.Clients.Group(NotificationHub.BuildTenantGroup(eventData.TenantId ?? Guid.Empty))
            .SendAsync("notificationLogCreated", eventData);

        if (eventData.UserId.HasValue)
        {
            await hubContext.Clients.Group(NotificationHub.BuildUserGroup(eventData.UserId.Value.ToString()))
                .SendAsync("notificationLogCreated", eventData);
        }
    }
}
