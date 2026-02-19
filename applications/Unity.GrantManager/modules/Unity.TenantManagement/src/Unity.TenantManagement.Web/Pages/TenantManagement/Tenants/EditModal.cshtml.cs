using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Unity.GrantManager.Integrations;
using Volo.Abp.Domain.Entities;
using Volo.Abp.ObjectExtending;
using Volo.Abp.TenantManagement;
using Volo.Abp.Validation;

namespace Unity.TenantManagement.Web.Pages.TenantManagement.Tenants;

public class EditModalModel(ITenantAppService tenantAppService, ICasClientCodeLookupService lookupService) : TenantManagementPageModel
{
    [BindProperty]
    public TenantInfoModel Tenant { get; set; }


    public List<CasClientCodeOptionDto> CasClientOptions { get; set; } = [];

    public virtual async Task<IActionResult> OnGetAsync(Guid id)
    {
        Tenant = ObjectMapper.Map<TenantDto, TenantInfoModel>(
            await tenantAppService.GetAsync(id)
        );

        CasClientOptions = await lookupService.GetActiveOptionsAsync();

        return Page();
    }

    public virtual async Task<IActionResult> OnPostAsync()
    {
        ValidateModel();

        var input = ObjectMapper.Map<TenantInfoModel, TenantUpdateDto>(Tenant);
        await tenantAppService.UpdateAsync(Tenant.Id, input);

        return NoContent();
    }

    public class TenantInfoModel : ExtensibleObject, IHasConcurrencyStamp
    {
        [HiddenInput]
        public Guid Id { get; set; }

        [Required]
        [DynamicStringLength(typeof(TenantConsts), nameof(TenantConsts.MaxNameLength))]
        [Display(Name = "DisplayName:TenantName")]
        public string Name { get; set; }
        public string Division { get; set; } = string.Empty;
        public string Branch { get; set; }  = string.Empty;
        public string Description { get; set; } = string.Empty;
        
        [Display(Name = "CAS Client Code")]
        public string CasClientCode { get; set; } = string.Empty;

        [HiddenInput]
        public string ConcurrencyStamp { get; set; }
    }
}
