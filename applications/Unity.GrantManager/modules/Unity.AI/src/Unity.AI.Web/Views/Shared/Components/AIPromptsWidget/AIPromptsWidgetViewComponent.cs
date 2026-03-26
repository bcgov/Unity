using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.AI.Web.Views.Shared.Components.AIPromptsWidget;

[ViewComponent(Name = "AIPromptsWidget")]
public class AIPromptsWidgetViewComponent : AbpViewComponent
{
    public IViewComponentResult Invoke()
    {
        return View();
    }
}
