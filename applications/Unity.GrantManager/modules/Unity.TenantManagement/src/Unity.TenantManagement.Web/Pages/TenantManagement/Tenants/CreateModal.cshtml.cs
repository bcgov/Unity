using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.ObjectExtending;
using Volo.Abp.TenantManagement;
using Volo.Abp.Validation;

namespace Unity.TenantManagement.Web.Pages.TenantManagement.Tenants;

public class CreateModalModel : TenantManagementPageModel
{
    [BindProperty]
    public TenantInfoModel Tenant { get; set; }

    protected ITenantAppService TenantAppService { get; }

    public CreateModalModel(ITenantAppService tenantAppService)
    {
        TenantAppService = tenantAppService;
    }

    public virtual Task<IActionResult> OnGetAsync()
    {
        Tenant = new TenantInfoModel();
        return Task.FromResult<IActionResult>(Page());
    }

    public virtual async Task<IActionResult> OnPostAsync()
    {
        ValidateModel();

        var input = ObjectMapper.Map<TenantInfoModel, TenantCreateDto>(Tenant);
        await TenantAppService.CreateAsync(input);

        return NoContent();
    }

    public class TenantInfoModel : ExtensibleObject
    {
        [Required]
        [DynamicStringLength(typeof(TenantConsts), nameof(TenantConsts.MaxNameLength))]
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
