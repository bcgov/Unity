using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;
using Volo.Abp.SecurityLog;
using Volo.Abp.Users;
using Volo.Abp.Security.Claims;
using System.Linq;

namespace Unity.GrantManager.Web.Pages.Account
{
    public class LogoutModel : PageModel
    {
        private readonly ISecurityLogManager _securityLogManager;
        private readonly ICurrentUser _currentUser;
        private readonly ICurrentPrincipalAccessor _currenPrincipalAccessor;

        public LogoutModel(ISecurityLogManager securityLogManager,
            ICurrentUser currentUser,
            ICurrentPrincipalAccessor currenPrincipalAccessor)
        {
            _securityLogManager = securityLogManager;
            _currentUser = currentUser;
            _currenPrincipalAccessor = currenPrincipalAccessor;
        }

        public async Task OnGetAsync()
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
            Response.Redirect("/");
        }
    }
}
