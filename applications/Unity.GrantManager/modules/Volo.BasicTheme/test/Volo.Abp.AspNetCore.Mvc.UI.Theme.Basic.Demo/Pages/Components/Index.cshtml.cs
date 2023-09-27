using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.UI.Navigation;

namespace Volo.Abp.AspNetCore.Mvc.UI.Theme.Basic.Demo.Pages.Components;

public class IndexModel : AbpPageModel
{
    public readonly IMenuManager _menuManager;

    public IndexModel(IMenuManager menuManager)
    {
        _menuManager = menuManager;
    }

    public void OnGet()
    {
        //Placeholder. Nothing to do here.
    }
}
