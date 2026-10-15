#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Unity.GrantManager.Integrations;
using Unity.Modules.Shared.Permissions;
using Volo.Abp.ObjectExtending;
using Volo.Abp.TenantManagement;
using Volo.Abp.Validation;

namespace Unity.TenantManagement.Web.Pages.TenantManagement.Tenants;

public class CreateModalModel : TenantManagementPageModel
{
    [BindProperty]
    public TenantInfoModel Tenant { get; set; } = null!;

    public List<CasClientCodeOptionDto> CasClientOptions { get; set; } = [];

    public bool CanManageFeatures { get; set; }

    protected ITenantAppService TenantAppService { get; }
    protected ICasClientCodeLookupService LookupService { get; }

    public CreateModalModel(ITenantAppService tenantAppService, ICasClientCodeLookupService lookupService)
    {
        TenantAppService = tenantAppService;
        LookupService = lookupService;
    }

    public virtual async Task<IActionResult> OnGetAsync()
    {
        Tenant = new TenantInfoModel();
        CasClientOptions = await LookupService.GetActiveOptionsAsync();

        CanManageFeatures = (await AuthorizationService
            .AuthorizeAsync(User, IdentityConsts.ITAdminOrITOperationsPolicyName)).Succeeded;

        return Page();
    }

    public virtual async Task<IActionResult> OnPostAsync()
    {
        ValidateModel();

        // The Features tab is only hidden client-side for non-IT-Admin/Ops callers, and
        // TenantAppService.CreateAsync itself is reachable by anyone with plain Tenants.Create
        // permission (TenantsCreateOrITOps) - it now re-checks this same policy and strips these
        // privileged fields itself, so this is defense in depth (a clean 4xx-free UX for this
        // page), not the only guard. Mirrors ConfigurationModalModel's FeaturesJson guard.
        var canManageFeatures = (await AuthorizationService
            .AuthorizeAsync(User, IdentityConsts.ITAdminOrITOperationsPolicyName)).Succeeded;

        if (!canManageFeatures)
        {
            Tenant.FeatureKeys = null;
        }

        var input = ObjectMapper.Map<TenantInfoModel, TenantCreateDto>(Tenant);
        await TenantAppService.CreateAsync(input);

        return NoContent();
    }

    public class TenantInfoModel : ExtensibleObject
    {
        [Required]
        [DynamicStringLength(typeof(TenantConsts), nameof(TenantConsts.MaxNameLength))]
        [Display(Name = "DisplayName:TenantName")]
        public string Name { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;
        public string Division { get; set; } = string.Empty;
        public string Branch { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        [Display(Name = "CAS Client Code")]
        public string? CasClientCode { get; set; }

        public string? FeatureKeys { get; set; }

        [Required]
        public string UserIdentifier { get; set; } = string.Empty;
    }
}
