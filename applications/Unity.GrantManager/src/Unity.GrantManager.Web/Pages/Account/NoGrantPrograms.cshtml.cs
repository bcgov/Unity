using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;

namespace Unity.GrantManager.Web.Pages.Errors
{
    public class IndexModel : PageModel
    {
        public async Task OnGetAsync()
        {
            // Clear out cookies so we dont get stuck here once access is granted
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);            
        }
    }
}
