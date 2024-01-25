using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.ObjectExtending;

namespace Unity.TenantManagement.Web.Pages.TenantManagement.Tenants;

public class AssignManagerModalModel : TenantManagementPageModel
{
    [BindProperty]
    public AssignManagerInfoModel Tenant { get; set; }

    protected ITenantAppService TenantAppService { get; }    

    public AssignManagerModalModel(ITenantAppService tenantAppService)
    {
        TenantAppService = tenantAppService;
    }

    public async virtual Task<IActionResult> OnGetAsync(Guid id)
    {
        Tenant = ObjectMapper.Map<TenantDto, AssignManagerInfoModel>(
            await TenantAppService.GetAsync(id)
        );

        return Page();
    }

    public virtual async Task<IActionResult> OnPostAsync()
    {
        ValidateModel();

        await TenantAppService.AssignManagerAsync(new TenantAssignManagerDto() 
        { 
            TenantId = Tenant.Id,
            UserIdentifier = Tenant.UserIdentifier
        });

        return NoContent();
    }

    public class AssignManagerInfoModel : ExtensibleObject
    {
        [HiddenInput]
        public Guid Id { get; set; }

        [HiddenInput]
        public string ConcurrencyStamp { get; set; }

        [Display(Name = "DisplayName:TenantName")]
        public string Name { get; set; }

        [DisplayName("First Name")]
        [MinLength(2, ErrorMessage = "At least 2 characters are required")]
        public string FirstName { get; set; }

        [DisplayName("Last Name")]
        [MinLength(2, ErrorMessage = "At least 2 characters are required")]
        public string LastName { get; set; }

        [Required]
        public string Directory { get; set; } = "IDIR";

        [Required]
        public string UserIdentifier { get; set; } = string.Empty;
    }
}
