using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Layout;

namespace Unity.AspNetCore.Mvc.UI.Theme.UX2.Themes.Standard.Components.ContentTitle;

public class ContentTitleViewComponent : AbpViewComponent
{
    protected IPageLayout UnityPageLayout { get; }

    public ContentTitleViewComponent(IPageLayout pageLayout)
    {
        UnityPageLayout = pageLayout;
    }

    public virtual IViewComponentResult Invoke()
    {
        return View("~/themes/Standard/Components/ContentTitle/Default.cshtml", UnityPageLayout.Content);
    }
}
