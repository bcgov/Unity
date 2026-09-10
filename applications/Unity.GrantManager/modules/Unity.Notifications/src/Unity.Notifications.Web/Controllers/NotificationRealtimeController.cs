using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Unity.Notifications.Features;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Features;

namespace Unity.Notifications.Web.Controllers;

[RemoteService(true)]
[Authorize]
[Route("api/notifications/realtime")]
public class NotificationRealtimeController(IFeatureChecker featureChecker) : AbpControllerBase
{
    [HttpGet("feature-enabled")]
    public async Task<bool> IsFeatureEnabledAsync()
    {
        return await featureChecker.IsEnabledAsync(NotificationsFeatureConsts.DirectMessaging);
    }
}