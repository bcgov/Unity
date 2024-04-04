using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.UI.Navigation;

namespace Unity.AspNetCore.Mvc.UI.Theme.UX2.Themes.UX2.Components.Toolbar.UserMenu;

public class UserMenuViewComponent : AbpViewComponent
{
    protected IMenuManager MenuManager { get; }

    public UserMenuViewComponent(IMenuManager menuManager)
    {
        MenuManager = menuManager;
    }

    public virtual async Task<IViewComponentResult> InvokeAsync()
    {
        var menu = await MenuManager.GetAsync(StandardMenus.User);
        return View("~/Themes/UX2/Components/Toolbar/UserMenu/Default.cshtml", menu);
    }
}
