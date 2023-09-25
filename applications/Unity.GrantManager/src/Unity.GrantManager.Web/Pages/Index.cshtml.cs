using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using System.Threading.Tasks;

namespace Unity.GrantManager.Web.Pages;

public class IndexModel : GrantManagerPageModel
{
    public IndexModel()
    {
    }

    public void  OnGet()
    {
        //Placeholder. Nothing to do here yet.
    }

    public async Task OnPostCancelAsync()
    {
        // Needs to be called to sign out
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
    }
}
