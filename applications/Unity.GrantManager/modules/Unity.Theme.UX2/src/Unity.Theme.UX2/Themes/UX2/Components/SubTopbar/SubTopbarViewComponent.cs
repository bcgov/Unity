using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Layout;

namespace Unity.AspNetCore.Mvc.UI.Theme.UX2.Themes.UX2.Components.MainNavbar;

public class SubTopbarViewComponent : AbpViewComponent
{
    protected IPageLayout PageLayout { get; }

    public SubTopbarViewComponent(IPageLayout pageLayout)
    {
        PageLayout = pageLayout;
    }

    public virtual IViewComponentResult Invoke()
    {
        return View("~/Themes/UX2/Components/SubTopbar/Default.cshtml", PageLayout.Content);
    }
}
