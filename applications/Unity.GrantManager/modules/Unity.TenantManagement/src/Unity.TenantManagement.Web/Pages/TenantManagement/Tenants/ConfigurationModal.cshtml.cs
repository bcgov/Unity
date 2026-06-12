#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Unity.GrantManager.Integrations;
using Unity.Modules.Shared.Permissions;
using Volo.Abp.Domain.Entities;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Features;
using Volo.Abp.ObjectExtending;
using Volo.Abp.TenantManagement;
using Volo.Abp.Validation;

namespace Unity.TenantManagement.Web.Pages.TenantManagement.Tenants;

public class ConfigurationModalModel(
    ITenantAppService tenantAppService,
    ICasClientCodeLookupService lookupService,
    IFeatureAppService featureAppService) : TenantManagementPageModel
{
    [BindProperty]
    public TenantInfoModel Tenant { get; set; } = null!;

    [BindProperty]
    public TenantConnectionStringsDto ConnectionStrings { get; set; } = new();

    [BindProperty]
    public string? SelectedManagerUserIdentifier { get; set; }

    [BindProperty]
    public string? FeaturesJson { get; set; }

    public ManagerInfoModel Manager { get; set; } = null!;

    public List<CasClientCodeOptionDto> CasClientOptions { get; set; } = [];

    public bool CanManageConnectionStrings { get; set; }

    public bool CanManageFeatures { get; set; }

    public bool CanManageManagers { get; set; }

    public virtual async Task<IActionResult> OnGetAsync(Guid id)
    {
        var tenantDto = await tenantAppService.GetAsync(id);

        Tenant = ObjectMapper.Map<TenantDto, TenantInfoModel>(tenantDto);
        Manager = ObjectMapper.Map<TenantDto, ManagerInfoModel>(tenantDto);
        CasClientOptions = await lookupService.GetActiveOptionsAsync();

        CanManageConnectionStrings = (await AuthorizationService
            .AuthorizeAsync(User, TenantManagementPermissions.Tenants.ManageConnectionStrings)).Succeeded;

        CanManageFeatures = (await AuthorizationService
            .AuthorizeAsync(User, IdentityConsts.ITOperationsPolicyName)).Succeeded;

        CanManageManagers = CanManageFeatures;

        if (CanManageConnectionStrings)
        {
            ConnectionStrings = await tenantAppService.GetConnectionStringsAsync(id);
        }

        return Page();
    }

    public virtual async Task<IActionResult> OnPostAsync()
    {
        ValidateModel();

        var input = ObjectMapper.Map<TenantInfoModel, TenantUpdateDto>(Tenant);
        await tenantAppService.UpdateAsync(Tenant.Id, input);

        if (!string.IsNullOrWhiteSpace(SelectedManagerUserIdentifier))
        {
            await tenantAppService.AssignManagerAsync(new TenantAssignManagerDto
            {
                TenantId = Tenant.Id,
                UserIdentifier = SelectedManagerUserIdentifier
            });
        }

        if ((await AuthorizationService.AuthorizeAsync(User, TenantManagementPermissions.Tenants.ManageConnectionStrings)).Succeeded)
        {
            await tenantAppService.UpdateConnectionStringsAsync(Tenant.Id, ConnectionStrings);
        }

        if (!string.IsNullOrEmpty(FeaturesJson) &&
            (await AuthorizationService.AuthorizeAsync(User, IdentityConsts.ITOperationsPolicyName)).Succeeded)
        {
            var featureUpdates = new List<UpdateFeatureDto>();
            foreach (var feature in JsonDocument.Parse(FeaturesJson).RootElement.EnumerateArray())
            {
                featureUpdates.Add(new UpdateFeatureDto
                {
                    Name = feature.GetProperty("name").GetString()!,
                    Value = feature.GetProperty("value").GetString()!
                });
            }
            await featureAppService.UpdateAsync(
                TenantFeatureValueProvider.ProviderName,
                Tenant.Id.ToString(),
                new UpdateFeaturesDto { Features = featureUpdates });
        }

        return NoContent();
    }

    public class TenantInfoModel : ExtensibleObject, IHasConcurrencyStamp
    {
        [HiddenInput]
        public Guid Id { get; set; }

        [Required]
        [DynamicStringLength(typeof(TenantConsts), nameof(TenantConsts.MaxNameLength))]
        [Display(Name = "DisplayName:TenantName")]
        public string Name { get; set; } = string.Empty;

        public string Division { get; set; } = string.Empty;
        public string Branch { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        [Display(Name = "CAS Client Code")]
        public string? CasClientCode { get; set; }

        [HiddenInput]
        public string ConcurrencyStamp { get; set; } = string.Empty;
    }

    public class ManagerInfoModel : ExtensibleObject
    {
        [HiddenInput]
        public Guid Id { get; set; }

        [Display(Name = "DisplayName:TenantName")]
        public string Name { get; set; } = string.Empty;

        [DisplayName("First Name")]
        public string? FirstName { get; set; }

        [DisplayName("Last Name")]
        public string? LastName { get; set; }

        [Required]
        public string Directory { get; set; } = "IDIR";

        [Required]
        public string UserIdentifier { get; set; } = string.Empty;
    }
}
