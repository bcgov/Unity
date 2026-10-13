using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Unity.GrantManager.Identity;
using Volo.Abp.Data;
using Volo.Abp.Identity;

namespace Unity.Identity.Web.Pages.Identity.Users;

public class RenameModalModel : IdentityPageModel
{
    [BindProperty]
    public RenameUserViewModel RenameInfo { get; set; }

    protected IIdentityUserAppService IdentityUserAppService { get; }

    private readonly IUserImportAppService _userImportAppService;
    private readonly IPersonRepository _personRepository;

    public RenameModalModel(
        IIdentityUserAppService identityUserAppService,
        IUserImportAppService userImportAppService,
        IPersonRepository personRepository)
    {
        IdentityUserAppService = identityUserAppService;
        _userImportAppService = userImportAppService;
        _personRepository = personRepository;
    }

    public virtual async Task<IActionResult> OnGetAsync(Guid userId)
    {
        var user = await IdentityUserAppService.GetAsync(userId);
        RenameInfo = new RenameUserViewModel
        {
            UserId = user.Id,
            UserName = user.UserName,
            FirstName = user.Name,
            LastName = user.Surname,
            Email = user.Email,
            DisplayName = user.GetProperty<string>("DisplayName") ?? string.Empty
        };
        RenameInfo.Badge = (await _personRepository.GetAsync(userId)).Badge;

        return Page();
    }

    public virtual async Task<IActionResult> OnPostAsync()
    {
        if (RenameInfo == null || RenameInfo.UserId == Guid.Empty || string.IsNullOrWhiteSpace(RenameInfo.UserIdentifier))
        {
            throw new Volo.Abp.Validation.AbpValidationException();
        }

        await _userImportAppService.RenameUserAsync(new RenameUserDto
        {
            UserId = RenameInfo.UserId,
            NewUserGuid = RenameInfo.UserIdentifier
        });

        return new JsonResult(new { });
    }

    public class RenameUserViewModel
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string DisplayName { get; set; }
        public string Badge { get; set; }
        public string UserIdentifier { get; set; } = string.Empty;
    }
}
