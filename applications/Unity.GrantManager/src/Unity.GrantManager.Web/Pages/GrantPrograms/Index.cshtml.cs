using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Volo.Abp.Security.Claims;

namespace Unity.GrantManager.Web.Pages.GrantPrograms
{
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
            // Try update claims principal on the fly? - seems to cause some issues, so far only reliable way to do this is signout and go through the auth process again            
            if (_httpContextAccessor != null && _httpContextAccessor.HttpContext != null)
            {
                await _httpContextAccessor.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                await _httpContextAccessor.HttpContext.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
            }

            if (_currentPrincipalAccessor != null && _currentPrincipalAccessor.Principal != null)
            {
                Response.Cookies.Append("set_tenant", SwapTenantId?.ToString() ?? Guid.Empty.ToString(), new CookieOptions()
                { Secure = true, SameSite = SameSiteMode.None, HttpOnly = true });                
            }            

            return Redirect("/GrantApplications");
        }
    }
}
