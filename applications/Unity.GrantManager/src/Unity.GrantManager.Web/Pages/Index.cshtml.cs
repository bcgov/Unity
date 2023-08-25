using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using Volo.Abp.Authorization.Permissions;

namespace Unity.GrantManager.Web.Pages;

public class IndexModel : GrantManagerPageModel
{
    public IndexModel()
    {
    }

    public void  OnGet()
    {
    }

    public async Task OnPostCancelAsync()
    {
        // Needs to be called to sign out
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
    }
}
