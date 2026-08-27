using Microsoft.AspNetCore.Authorization;
using Unity.Modules.Shared.Permissions;

namespace Unity.TenantManagement.Web.Pages.TenantManagement.Onboarding;

[Authorize(IdentityConsts.ITOperationsPolicyName)]
public class IndexModel : OnboardingPageModel
{
    public void OnGet() { }
}
