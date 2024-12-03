using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;

namespace Unity.Notifications.Web.Settings.NotificationsSettingGroup;

[Widget(
    ScriptTypes = [typeof(NotificationsSettingScriptBundleContributor)],
    AutoInitialize = true
)]
[ViewComponent(Name = "NotificationsSetting")]
public class NotificationsSettingViewComponent : AbpViewComponent
{
    public virtual async Task<IViewComponentResult> InvokeAsync()
    {
        // Debugging mock
        var model = new NotificationsSettingViewModel();
        model.DefaultFromAddress = "noreply@gov.bc.ca";
        model.DefaultFromDisplayName = "BC Gov NoReply";
        return View("~/Settings/NotificationsSettingGroup/Default.cshtml", model);
    }
}
