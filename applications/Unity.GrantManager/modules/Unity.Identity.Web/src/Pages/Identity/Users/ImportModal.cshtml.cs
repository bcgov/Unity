using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.ObjectExtending;
using Volo.Abp.Identity;

namespace Unity.Identity.Web.Pages.Identity.Users;

public class ImportModalModel : IdentityPageModel
{
    [BindProperty]
    public UserImportViewModel UserInfo { get; set; }

    [BindProperty]
    public AssignedRoleViewModel[] Roles { get; set; }

    protected IIdentityUserAppService IdentityUserAppService { get; }

    public ImportModalModel(IIdentityUserAppService identityUserAppService)
    {
        IdentityUserAppService = identityUserAppService;
    }

    public virtual async Task<IActionResult> OnGetAsync()
    {
        UserInfo = new UserImportViewModel();

        var roleDtoList = (await IdentityUserAppService.GetAssignableRolesAsync()).Items;

        Roles = ObjectMapper.Map<IReadOnlyList<IdentityRoleDto>, AssignedRoleViewModel[]>(roleDtoList);

        foreach (var role in Roles)
        {
            role.IsAssigned = role.IsDefault;
        }

        return Page();
    }

    public virtual async Task<NoContentResult> OnPostAsync()
    {
        ValidateModel();

        var input = ObjectMapper.Map<UserImportViewModel, IdentityUserCreateDto>(UserInfo);
        input.RoleNames = Roles.Where(r => r.IsAssigned).Select(r => r.Name).ToArray();

        await IdentityUserAppService.CreateAsync(input);

        return NoContent();
    }

    public class UserImportViewModel : ExtensibleObject
    {
        [MinLength(2)]
        public string FirstName { get; set; }

        [MinLength(2)]
        public string LastName { get; set; }

        [Required]
        public string Directory { get; set; } = "IDIR";
    }

    public class AssignedRoleViewModel
    {
        [Required]
        [HiddenInput]
        public string Name { get; set; }

        public bool IsAssigned { get; set; }

        public bool IsDefault { get; set; }
    }
}
