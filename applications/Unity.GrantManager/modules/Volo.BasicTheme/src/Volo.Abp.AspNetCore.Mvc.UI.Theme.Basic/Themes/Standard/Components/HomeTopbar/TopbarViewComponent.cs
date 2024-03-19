using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.AspNetCore.Mvc.UI.Themes.Standard.Components.HomeNavbar;

public class HomeTopbarViewComponent : AbpViewComponent
{
    public virtual IViewComponentResult Invoke()
    {
        return View("~/Themes/Standard/Components/HomeTopbar/Default.cshtml");
    }
}
