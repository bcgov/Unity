using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Unity.GrantManager.Web.Menus;

namespace Unity.GrantManager.Web.Pages.Account
{
    [AllowAnonymous]
    public class AccessDeniedModel : PageModel
    {
        public IActionResult OnGet() => LocalRedirect(UserNavigationService.NoAccessUrl);
    }
}
