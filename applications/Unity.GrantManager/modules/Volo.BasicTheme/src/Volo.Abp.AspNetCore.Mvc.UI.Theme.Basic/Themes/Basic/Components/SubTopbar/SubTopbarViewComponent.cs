using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Layout;

namespace Volo.Abp.AspNetCore.Mvc.UI.Theme.Basic.Themes.Basic.Components.MainNavbar;

public class SubTopbarViewComponent : AbpViewComponent
{
    protected IPageLayout PageLayout { get; }

    public SubTopbarViewComponent(IPageLayout pageLayout)
    {
        PageLayout = pageLayout;
    }

    public virtual IViewComponentResult Invoke()
    {
        return View("~/Themes/Basic/Components/SubTopbar/Default.cshtml", PageLayout.Content);
    }
}
