using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Volo.Abp.Security.Claims;

namespace Unity.GrantManager.Web.Pages.GrantPrograms
{
    [Authorize]
    public class IndexModel : GrantManagerPageModel
    {
        [BindProperty]
        public Guid? SwapTenantId { get; set; }

        private readonly ICurrentPrincipalAccessor _currentPrincipalAccessor;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public IndexModel(ICurrentPrincipalAccessor currentPrincipalAccessor,
            IHttpContextAccessor httpContextAccessor)
        {
            _currentPrincipalAccessor = currentPrincipalAccessor;
            _httpContextAccessor = httpContextAccessor;
        }

        public void OnGet()
        {
            // Method intentionally left empty.
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Signout and re-auth is the only reliable way to swap tenant claims
            if (_httpContextAccessor?.HttpContext != null)
            {
                await _httpContextAccessor.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                await _httpContextAccessor.HttpContext.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
            }

            if (_currentPrincipalAccessor?.Principal != null)
            {
                Response.Cookies.Append("set_tenant", SwapTenantId?.ToString() ?? Guid.Empty.ToString(), new CookieOptions()
                { Secure = true, SameSite = SameSiteMode.None, HttpOnly = true });
            }

            // Evaluate navigation after re-authentication, using the target tenant's user
            // and permissions rather than the identity from the previous tenant.
            return Redirect("/Account/Login");
        }
    }
}
