using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;

namespace Unity.GrantManager.Web.Views.Shared.Components.Notifications;

[ViewComponent(Name = "Notifications")]
[Widget(
    ScriptFiles = ["/Views/Shared/Components/Notifications/Default.js"],
    StyleFiles = ["/Views/Shared/Components/Notifications/Default.css"],
    RefreshUrl = "Widgets/Notifications/Refresh",
    AutoInitialize = true
)]
public class Notifications : AbpViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(string? formid)
    {
        await Task.CompletedTask;

        var viewModel = new NotificationsViewModel()
        {
            FormId = formid
        };

        return View(viewModel);
    }
}
