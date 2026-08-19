using System;
using Unity.Notifications.EntityFrameworkCore;
using Unity.Notifications.Logs;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace Unity.Notifications.Repositories;

public class NotificationLogsRepository(IDbContextProvider<NotificationsDbContext> dbContextProvider)
    : EfCoreRepository<NotificationsDbContext, NotificationLog, Guid>(dbContextProvider), INotificationLogsRepository
{
}