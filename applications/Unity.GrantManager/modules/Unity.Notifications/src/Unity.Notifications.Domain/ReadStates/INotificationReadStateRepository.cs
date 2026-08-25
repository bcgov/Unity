using System;
using Volo.Abp.Domain.Repositories;

namespace Unity.Notifications.ReadStates;

public interface INotificationReadStateRepository : IRepository<NotificationReadState, Guid>
{
}
