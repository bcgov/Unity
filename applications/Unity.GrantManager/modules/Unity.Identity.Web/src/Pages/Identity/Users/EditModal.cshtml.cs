using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Domain.Entities;
using Volo.Abp.ObjectExtending;
using Volo.Abp.Identity;
using System.ComponentModel;
using Volo.Abp.Validation;
using Volo.Abp;
using Volo.Abp.Data;

namespace Unity.Identity.Web.Pages.Identity.Users;

public class EditModalModel : IdentityPageModel
{
    [BindProperty]
    public UserInfoViewModel UserInfo { get; set; }

    [BindProperty]
    public AssignedRoleViewModel[] Roles { get; set; }

    public DetailViewModel Detail { get; set; }

    protected IIdentityUserAppService IdentityUserAppService { get; }

    public bool IsEditCurrentUser { get; set; }

    private readonly IDataFilter _dataFilter;

    public EditModalModel(IIdentityUserAppService identityUserAppService,
        IDataFilter dataFilter)
    {
        IdentityUserAppService = identityUserAppService;
        _dataFilter = dataFilter;
    }

    public virtual async Task<IActionResult> OnGetAsync(Guid id)
    {
        var user = await IdentityUserAppService.GetAsync(id);
        UserInfo = ObjectMapper.Map<IdentityUserDto, UserInfoViewModel>(user);
        Roles = ObjectMapper.Map<IReadOnlyList<IdentityRoleDto>, AssignedRoleViewModel[]>((await IdentityUserAppService.GetAssignableRolesAsync()).Items);
        IsEditCurrentUser = CurrentUser.Id == id;

        var userRoleNames = (await IdentityUserAppService.GetRolesAsync(UserInfo.Id)).Items.Select(r => r.Name).ToList();
        foreach (var role in Roles)
        {
            if (userRoleNames.Contains(role.Name))
            {
                role.IsAssigned = true;
            }
        }

        Detail = ObjectMapper.Map<IdentityUserDto, DetailViewModel>(user);

        Detail.CreatedBy = await GetUserNameOrNullAsync(user.CreatorId);
        Detail.ModifiedBy = await GetUserNameOrNullAsync(user.LastModifierId);

        return Page();
    }

    private async Task<string> GetUserNameOrNullAsync(Guid? userId)
    {
        if (!userId.HasValue)
        {
            return null;
        }

        using (_dataFilter.Disable<ISoftDelete>())
        {
            var user = await IdentityUserAppService.GetAsync(userId.Value);
            return user.UserName;
        }
    }

    public virtual async Task<IActionResult> OnPostAsync()
    {
        ValidateModel();

        var input = ObjectMapper.Map<UserInfoViewModel, IdentityUserUpdateDto>(UserInfo);
        input.RoleNames = Roles.Where(r => r.IsAssigned).Select(r => r.Name).ToArray();
        await IdentityUserAppService.UpdateRolesAsync(UserInfo.Id, new IdentityUserUpdateRolesDto() { RoleNames = input.RoleNames });

        return NoContent();
    }

    public class UserInfoViewModel : ExtensibleObject, IHasConcurrencyStamp
    {
        [HiddenInput]
        public Guid Id { get; set; }

        [HiddenInput]
        public string ConcurrencyStamp { get; set; }

        [ReadOnly(true)]
        [DisableValidation]
        public string UserName { get; set; }

        [ReadOnly(true)]
        public string Name { get; set; }

        [ReadOnly(true)]
        public string Surname { get; set; }

        [ReadOnly(true)]
        [DisableValidation]
        public string Email { get; set; }
    }

    public class AssignedRoleViewModel
    {
        [Required]
        [HiddenInput]
        public string Name { get; set; }
        public bool IsAssigned { get; set; }
    }

    public class DetailViewModel
    {
        public string CreatedBy { get; set; }
        public DateTime? CreationTime { get; set; }
        public string ModifiedBy { get; set; }
        public DateTime? LastModificationTime { get; set; }
    }
}
