using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.UI.Navigation;

namespace Volo.Abp.AspNetCore.Mvc.UI.Theme.Basic.Themes.Basic.Components.MainNavbar;

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
        return View("~/Themes/Basic/Components/SideNavbar/Default.cshtml", menu);
    }
}
