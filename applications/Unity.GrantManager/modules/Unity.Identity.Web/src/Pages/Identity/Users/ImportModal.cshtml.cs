using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.ObjectExtending;
using Unity.GrantManager.Identity;
using System.ComponentModel;

namespace Unity.Identity.Web.Pages.Identity.Users;

public class ImportModalModel : IdentityPageModel
{
    [BindProperty]
    public UserImportViewModel UserInfo { get; set; }

    private readonly IUserImportAppService _userImportAppService;

    public ImportModalModel(IUserImportAppService userImportAppService)
    {
        _userImportAppService = userImportAppService;
    }

    public virtual async Task<IActionResult> OnGetAsync()
    {
        UserInfo = new UserImportViewModel();
        await Task.CompletedTask;
        return Page();
    }

    public virtual async Task<IActionResult> OnPostAsync()
    {
        ValidateModel();

        await _userImportAppService.ImportUserAsync(new ImportUserDto() { Directory = UserInfo.Directory, Guid = UserInfo.UserIdentifier });

        return new NoContentResult();
    }

    public class UserImportViewModel : ExtensibleObject
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
    }
}
