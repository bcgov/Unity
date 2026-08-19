using System;
using System.Threading.Tasks;

namespace Unity.Notifications.Logs;

public interface INotificationLogsAppService
{
    Task<Guid> CreateAsync(CreateNotificationLogDto input);
}