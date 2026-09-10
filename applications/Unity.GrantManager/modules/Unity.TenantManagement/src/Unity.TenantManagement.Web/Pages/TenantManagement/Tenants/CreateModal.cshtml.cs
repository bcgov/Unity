#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Unity.GrantManager.Integrations;
using Unity.Modules.Shared.Permissions;
using Unity.TenantManagement.Metabase;
using Volo.Abp.ObjectExtending;
using Volo.Abp.SettingManagement;
using Volo.Abp.TenantManagement;
using Volo.Abp.Validation;

namespace Unity.TenantManagement.Web.Pages.TenantManagement.Tenants;

public class CreateModalModel : TenantManagementPageModel
{
    [BindProperty]
    public TenantInfoModel Tenant { get; set; } = null!;

    public List<CasClientCodeOptionDto> CasClientOptions { get; set; } = [];

    public List<string> DefaultMetabaseUserEmails { get; set; } = [];

    public bool CanManageFeatures { get; set; }

    protected ITenantAppService TenantAppService { get; }
    protected ICasClientCodeLookupService LookupService { get; }
    protected ISettingManager SettingManager { get; }

    public CreateModalModel(ITenantAppService tenantAppService, ICasClientCodeLookupService lookupService, ISettingManager settingManager)
    {
        TenantAppService = tenantAppService;
        LookupService = lookupService;
        SettingManager = settingManager;
    }

    public virtual async Task<IActionResult> OnGetAsync()
    {
        Tenant = new TenantInfoModel();
        CasClientOptions = await LookupService.GetActiveOptionsAsync();
        DefaultMetabaseUserEmails = SplitEmails(await SettingManager.GetOrNullGlobalAsync(MetabaseSettings.UserEmails));

        CanManageFeatures = (await AuthorizationService
            .AuthorizeAsync(User, IdentityConsts.ITAdminOrITOperationsPolicyName)).Succeeded;

        return Page();
    }

    public virtual async Task<IActionResult> OnPostAsync()
    {
        ValidateModel();

        // The Features/Metabase tabs are only hidden client-side for non-IT-Admin/Ops callers, and
        // TenantAppService.CreateAsync itself is reachable by anyone with plain Tenants.Create
        // permission (TenantsCreateOrITOps) - it now re-checks this same policy and strips these
        // privileged fields itself, so this is defense in depth (a clean 4xx-free UX for this
        // page), not the only guard. Mirrors ConfigurationModalModel's FeaturesJson guard.
        var canManageFeatures = (await AuthorizationService
            .AuthorizeAsync(User, IdentityConsts.ITAdminOrITOperationsPolicyName)).Succeeded;

        if (!canManageFeatures)
        {
            Tenant.FeatureKeys = null;
            Tenant.MetabaseUserEmails = null;
            Tenant.MetabaseNewDefaultUserEmails = null;
            Tenant.MetabaseRemovedDefaultUserEmails = null;
        }

        var input = ObjectMapper.Map<TenantInfoModel, TenantCreateDto>(Tenant);
        await TenantAppService.CreateAsync(input);

        if (!string.IsNullOrWhiteSpace(Tenant.MetabaseNewDefaultUserEmails) || !string.IsNullOrWhiteSpace(Tenant.MetabaseRemovedDefaultUserEmails))
        {
            await UpdateMetabaseDefaultUserEmailsAsync(Tenant.MetabaseNewDefaultUserEmails, Tenant.MetabaseRemovedDefaultUserEmails);
        }

        return NoContent();
    }

    private async Task UpdateMetabaseDefaultUserEmailsAsync(string? newEmailsCsv, string? removedEmailsCsv)
    {
        var removed = SplitEmails(removedEmailsCsv);
        var updated = SplitEmails(await SettingManager.GetOrNullGlobalAsync(MetabaseSettings.UserEmails))
            .Concat(SplitEmails(newEmailsCsv))
            .Where(email => !removed.Contains(email, StringComparer.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase);

        await SettingManager.SetGlobalAsync(MetabaseSettings.UserEmails, string.Join(",", updated));
    }

    private static List<string> SplitEmails(string? emailsCsv) =>
        (emailsCsv ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

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

        /// <summary>Comma-separated emails checked in the Metabase tab - sent to TenantCreateDto as-is.</summary>
        public string? MetabaseUserEmails { get; set; }

        /// <summary>Comma-separated subset of newly-added Metabase emails to persist as the new Global default.</summary>
        public string? MetabaseNewDefaultUserEmails { get; set; }

        /// <summary>Comma-separated default Metabase emails explicitly removed - deleted from the Global default.</summary>
        public string? MetabaseRemovedDefaultUserEmails { get; set; }

        [Required]
        public string UserIdentifier { get; set; } = string.Empty;
    }
}
