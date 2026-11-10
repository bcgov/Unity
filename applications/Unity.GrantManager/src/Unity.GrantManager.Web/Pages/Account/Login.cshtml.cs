using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;
using Unity.GrantManager.Web.Menus;

namespace Unity.GrantManager.Web.Pages.Account
{
    [Authorize]
    public class LoginModel(UserNavigationService navigation) : PageModel
    {
        public async Task<IActionResult> OnGetAsync()
            => LocalRedirect(await navigation.GetLandingPageAsync() ?? UserNavigationService.NoAccessUrl);
    }
}
