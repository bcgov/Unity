using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;

namespace Unity.GrantManager.Web.Pages.Account
{
    [Authorize]
    public class LoginModel : PageModel
    {
        public async Task OnGetAsync(string? returnUrl = null)
        {
            Response.Redirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : "/GrantApplications");
            await Task.CompletedTask;
        }
    }
}
