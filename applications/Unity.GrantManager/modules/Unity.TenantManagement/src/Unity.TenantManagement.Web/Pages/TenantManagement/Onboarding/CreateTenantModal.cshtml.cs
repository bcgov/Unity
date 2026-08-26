#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Unity.Modules.Shared.Permissions;
using Unity.TenantManagement.Metabase;
using Volo.Abp.SettingManagement;

namespace Unity.TenantManagement.Web.Pages.TenantManagement.Onboarding;

[Authorize(IdentityConsts.ITOperationsPolicyName)]
public class CreateTenantModalModel(IOnboardingRequestAppService onboardingRequestAppService, ISettingManager settingManager)
    : OnboardingPageModel
{
    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    public OnboardingRequestDto? OnboardingRequest { get; set; }

    public List<string> DefaultMetabaseUserEmails { get; set; } = [];

    public virtual async Task<IActionResult> OnGetAsync()
    {
        OnboardingRequest = await onboardingRequestAppService.GetAsync(Id);
        if (OnboardingRequest == null) return NotFound();

        var defaultEmailsCsv = await settingManager.GetOrNullGlobalAsync(MetabaseSettings.UserEmails);
        DefaultMetabaseUserEmails = (defaultEmailsCsv ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        return Page();
    }
}
