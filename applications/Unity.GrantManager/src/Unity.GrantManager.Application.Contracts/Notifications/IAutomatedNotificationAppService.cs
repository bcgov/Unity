using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.Notifications
{
    public interface IAutomatedNotificationAppService : IApplicationService
    {
        Task<PagedResultDto<NotificationDto>> GetListAsync(GetNotificationsInput input);
        Task<NotificationDto> GetAsync(Guid id);
        Task<NotificationDto> CreateAsync(CreateUpdateNotificationDto input);
        Task<NotificationDto> UpdateAsync(Guid id, CreateUpdateNotificationDto input);
        Task DeleteAsync(Guid id);
        Task<NotificationTemplateDto[]> GetTemplatesAsync();
    }
}
