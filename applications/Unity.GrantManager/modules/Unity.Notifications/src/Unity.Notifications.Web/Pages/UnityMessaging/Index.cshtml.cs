using Microsoft.AspNetCore.Authorization;
using Unity.Modules.Shared.Permissions;
using Unity.Notifications.Features;
using Volo.Abp.Features;

namespace Unity.Notifications.Web.Pages.UnityMessaging;

[Authorize(IdentityConsts.ITOperationsPermissionName)]
[RequiresFeature(NotificationsFeatureConsts.DirectMessaging)]
public class IndexModel : NotificationsPageModel
{
    public void OnGet()
    {
    }
}
