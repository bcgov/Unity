using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.UI.Navigation;

namespace Unity.AspNetCore.Mvc.UI.Theme.UX2.Themes.UX2.Components.MainNavbar;

public class SideNavbarViewComponent : AbpViewComponent
{
    protected IMenuManager MenuManager { get; }

    public SideNavbarViewComponent(IMenuManager menuManager)
    {
        MenuManager = menuManager;
    }

    public virtual async Task<IViewComponentResult> InvokeAsync()
    {
        var menu = await MenuManager.GetMainMenuAsync();
        return View("~/Themes/UX2/Components/SideNavbar/Default.cshtml", menu);
    }
}
