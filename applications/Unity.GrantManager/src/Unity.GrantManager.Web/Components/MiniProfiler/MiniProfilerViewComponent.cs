using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.GrantManager.Web.Components.MiniProfiler;

public class MiniProfilerViewComponent : AbpViewComponent
{
    public virtual IViewComponentResult Invoke()
    {
        return View("~/Components/MiniProfiler/Default.cshtml");
    }
}



