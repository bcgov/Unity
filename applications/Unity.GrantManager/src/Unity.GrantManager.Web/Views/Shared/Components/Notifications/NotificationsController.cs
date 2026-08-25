using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.GrantManager.Web.Views.Shared.Components.Notifications;

[ApiExplorerSettings(IgnoreApi = true)]
[Route("GrantApplications/Widgets/Notifications")]
public class NotificationsController : AbpController
{
    [HttpGet]
    [Route("Refresh")]
    public IActionResult Refresh(string? formid)
    {
        if (!ModelState.IsValid)
        {
            return ViewComponent("Notifications");
        }
        return ViewComponent(typeof(Notifications), new { formid });
    }
}
