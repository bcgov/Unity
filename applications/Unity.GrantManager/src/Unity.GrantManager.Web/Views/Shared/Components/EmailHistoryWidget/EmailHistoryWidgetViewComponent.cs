using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;

namespace Unity.GrantManager.Web.Views.Shared.Components.EmailHistoryWidget;

[Widget(ScriptTypes = [typeof(EmailHistoryScriptBundleContributor)])]
public class EmailHistoryWidgetViewComponent : AbpViewComponent
{
    public IViewComponentResult Invoke()
    {
        return View();
    }
}
