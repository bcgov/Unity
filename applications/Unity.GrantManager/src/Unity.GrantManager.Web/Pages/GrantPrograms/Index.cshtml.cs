using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Unity.Modules.Shared.Specializations;
using Volo.Abp.Features;
using Volo.Abp.Security.Claims;

namespace Unity.GrantManager.Web.Pages.GrantPrograms
{
    public class IndexModel : GrantManagerPageModel
    {
        [BindProperty]
        public Guid? SwapTenantId { get; set; }

        private readonly ICurrentPrincipalAccessor _currentPrincipalAccessor;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IFeatureChecker _featureChecker;

        public IndexModel(ICurrentPrincipalAccessor currentPrincipalAccessor,
            IHttpContextAccessor httpContextAccessor,
            IFeatureChecker featureChecker)
        {
            _currentPrincipalAccessor = currentPrincipalAccessor;
            _httpContextAccessor = httpContextAccessor;
            _featureChecker = featureChecker;
        }

        public void OnGet()
        {
            // Method intentionally left empty.
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Determine landing page for the target tenant before signing out
            var redirectUrl = "/GrantApplications";
            if (SwapTenantId.HasValue)
            {
                using (CurrentTenant.Change(SwapTenantId))
                {
                    if (await _featureChecker.IsEnabledAsync(SpecializationConsts.Onboarding))
                    {
                        redirectUrl = "/TenantManagement/Onboarding";
                    }
                }
            }

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

            return Redirect(redirectUrl);
        }
    }
}
