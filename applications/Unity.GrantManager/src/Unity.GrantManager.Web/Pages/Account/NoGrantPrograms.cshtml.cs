using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Unity.GrantManager.Web.Pages.Account
{
    [AllowAnonymous]
    public class NoGrantProgramsModel : PageModel
    {
        public void OnGet()
        {
            // Keep an authenticated tenant user signed in so they can switch programs or
            // retry after permissions are granted. Unlinked identities are signed out at login.
        }
    }
}
