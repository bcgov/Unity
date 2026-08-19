using System;
using Volo.Abp.Domain.Repositories;

namespace Unity.Notifications.Logs;

public interface INotificationLogsRepository : IRepository<NotificationLog, Guid>
{
}