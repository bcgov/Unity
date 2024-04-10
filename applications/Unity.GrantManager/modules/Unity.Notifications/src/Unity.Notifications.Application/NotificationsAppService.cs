using Unity.Notifications.Localization;
using Volo.Abp.Application.Services;

namespace Unity.Notifications;

public abstract class NotificationsAppService : ApplicationService
{
    protected NotificationsAppService()
    {
        LocalizationResource = typeof(NotificationsResource);
        ObjectMapperContext = typeof(NotificationsApplicationModule);
    }
}
