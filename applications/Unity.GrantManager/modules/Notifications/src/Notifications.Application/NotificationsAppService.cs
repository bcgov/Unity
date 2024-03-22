using Notifications.Localization;
using Volo.Abp.Application.Services;

namespace Notifications;

public abstract class NotificationsAppService : ApplicationService
{
    protected NotificationsAppService()
    {
        LocalizationResource = typeof(NotificationsResource);
        ObjectMapperContext = typeof(NotificationsApplicationModule);
    }
}
