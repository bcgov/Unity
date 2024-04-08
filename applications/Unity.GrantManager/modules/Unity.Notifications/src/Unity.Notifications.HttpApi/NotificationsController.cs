using Unity.Notifications.Localization;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.Notifications;

public abstract class NotificationsController : AbpControllerBase
{
    protected NotificationsController()
    {
        LocalizationResource = typeof(NotificationsResource);
    }
}
