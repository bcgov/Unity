#nullable enable
using Microsoft.AspNetCore.Authorization;
using Unity.Modules.Shared.Permissions;

namespace Unity.TenantManagement.Web.Pages.TenantManagement.Onboarding;

// The selected rows live in the list page, and Onboarding/Index.js fills this modal from them.
[Authorize(IdentityConsts.ITOperationsPolicyName)]
public class AssignModalModel : OnboardingPageModel
{
    public virtual void OnGet()
    {
        // Nothing to load: users come from api/onboarding-requests/assignable-users.
    }
}
