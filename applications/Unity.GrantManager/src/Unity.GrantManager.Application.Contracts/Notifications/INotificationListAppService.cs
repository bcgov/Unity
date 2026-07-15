using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.Notifications;

public interface INotificationListAppService : IApplicationService
{
    Task<PagedResultDto<NotificationSummaryDto>> GetListAsync(NotificationListInputDto input);
}
