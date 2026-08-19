using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;

namespace Unity.Notifications.Logs;

public interface INotificationLogsReadAppService
{
    Task<PagedResultDto<NotificationLogListDto>> GetListAsync(GetNotificationLogsInput input);

    Task<NotificationLogDetailDto> GetAsync(Guid id);
}
