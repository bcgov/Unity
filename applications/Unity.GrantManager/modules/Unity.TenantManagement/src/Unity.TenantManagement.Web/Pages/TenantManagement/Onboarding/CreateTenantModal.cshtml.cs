#nullable enable
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Unity.Modules.Shared.Permissions;

namespace Unity.TenantManagement.Web.Pages.TenantManagement.Onboarding;

[Authorize(IdentityConsts.ITOperationsPolicyName)]
public class CreateTenantModalModel(IOnboardingRequestAppService onboardingRequestAppService)
    : OnboardingPageModel
{
    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    public OnboardingRequestDto? OnboardingRequest { get; set; }

    public virtual async Task<IActionResult> OnGetAsync()
    {
        OnboardingRequest = await onboardingRequestAppService.GetAsync(Id);
        if (OnboardingRequest == null) return NotFound();
        return Page();
    }
}
