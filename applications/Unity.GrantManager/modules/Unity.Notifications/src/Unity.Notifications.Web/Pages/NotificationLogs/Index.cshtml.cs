using Microsoft.AspNetCore.Authorization;
using Unity.Modules.Shared.Permissions;

namespace Unity.Notifications.Web.Pages.NotificationLogs;

[Authorize(IdentityConsts.ITOperationsPermissionName)]
public class IndexModel : NotificationsPageModel
{
    public void OnGet()
    {
    }
}
