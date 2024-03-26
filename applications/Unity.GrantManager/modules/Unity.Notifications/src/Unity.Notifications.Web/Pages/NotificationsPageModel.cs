using Unity.Notifications.Localization;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Unity.Notifications.Web.Pages;

/* Inherit your PageModel classes from this class.
 */
public abstract class NotificationsPageModel : AbpPageModel
{
    protected NotificationsPageModel()
    {
        LocalizationResourceType = typeof(NotificationsResource);
        ObjectMapperContext = typeof(NotificationsWebModule);
    }
}
