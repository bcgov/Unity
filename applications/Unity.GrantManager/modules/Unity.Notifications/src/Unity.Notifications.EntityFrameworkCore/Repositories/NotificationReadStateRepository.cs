using System;
using Unity.Notifications.EntityFrameworkCore;
using Unity.Notifications.ReadStates;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace Unity.Notifications.Repositories;

public class NotificationReadStateRepository(IDbContextProvider<NotificationsDbContext> dbContextProvider)
    : EfCoreRepository<NotificationsDbContext, NotificationReadState, Guid>(dbContextProvider), INotificationReadStateRepository
{
}
