using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Unity.GrantManager.Identity;
using Volo.Abp.ObjectExtending;

namespace Unity.TenantManagement.Web.Pages.TenantManagement.ITRoles;

public class AssignModalModel : ITRolesPageModel
{
    [BindProperty]
    public AssignRoleViewModel RoleInfo { get; set; }

    private readonly IHostRoleAppService _hostRoleAppService;

    public AssignModalModel(IHostRoleAppService hostRoleAppService)
    {
        _hostRoleAppService = hostRoleAppService;
    }

    public virtual async Task<IActionResult> OnGetAsync()
    {
        RoleInfo = new AssignRoleViewModel();
        await Task.CompletedTask;
        return Page();
    }

    public virtual async Task<NoContentResult> OnPostAsync()
    {
        ValidateModel();

        await _hostRoleAppService.AssignRoleAsync(new AssignHostRoleDto()
        {
            Directory = RoleInfo.Directory,
            Guid = RoleInfo.UserIdentifier,
            RoleNames = RoleInfo.RoleNames ?? []
        });

        return NoContent();
    }

    public class AssignRoleViewModel : ExtensibleObject
    {
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

        [MinLength(1, ErrorMessage = "Select at least one IT role")]
        public string[] RoleNames { get; set; } = [];
    }
}
