using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Security.Claims;
using Volo.Abp.SecurityLog;
using Volo.Abp.Users;

namespace Unity.GrantManager.Web.Pages;

public class IndexModel : GrantManagerPageModel
{
    private readonly ISecurityLogManager _securityLogManager;
    private readonly ICurrentUser _currentUser;
    private readonly ICurrentPrincipalAccessor _currenPrincipalAccessor;

    public IndexModel(ISecurityLogManager securityLogManager,
        ICurrentUser currentUser,
        ICurrentPrincipalAccessor currenPrincipalAccessor)
    {
        _securityLogManager = securityLogManager;
        _currentUser = currentUser;
        _currenPrincipalAccessor = currenPrincipalAccessor;
    }

    public void OnGet()
    {
        //Placeholder. Nothing to do here yet.
    }

    public async Task OnPostCancelAsync()
    {        
        // When using a different directory (i.e. not idir, see if this still applies to prefrerred_username)
        var identityClaim = _currenPrincipalAccessor.Principal.Claims.FirstOrDefault(s => s.Type == "preferred_username");

        await _securityLogManager.SaveAsync(securityLog =>
        {
            securityLog.Identity = identityClaim?.Value;
            securityLog.Action = "Logout";
            securityLog.UserId = _currentUser.Id;
            securityLog.UserName = _currentUser.UserName;
        });

        // Needs to be called to sign out
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
    }
}
