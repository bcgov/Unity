using Notifications.Localization;
using Volo.Abp.AspNetCore.Mvc;

namespace Notifications;

public abstract class NotificationsController : AbpControllerBase
{
    protected NotificationsController()
    {
        LocalizationResource = typeof(NotificationsResource);
    }
}
