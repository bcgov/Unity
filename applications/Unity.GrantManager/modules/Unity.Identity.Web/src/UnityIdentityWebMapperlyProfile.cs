using Riok.Mapperly.Abstractions;
using Unity.Identity.Web.Pages.Identity.Roles;
using Volo.Abp.Identity;
using Volo.Abp.Mapperly;
using static Unity.Identity.Web.Pages.Identity.Users.EditModalModel;
using EditRoleModal = Unity.Identity.Web.Pages.Identity.Roles.EditModalModel;
using CreateRoleModal = Unity.Identity.Web.Pages.Identity.Roles.CreateModalModel;

namespace Volo.Abp.Identity.Web;

internal static class IdentityExtraPropertiesCopier
{
    public static void Copy(Volo.Abp.Data.IHasExtraProperties source, Volo.Abp.Data.IHasExtraProperties destination)
    {
        foreach (var kvp in source.ExtraProperties)
        {
            destination.ExtraProperties[kvp.Key] = kvp.Value;
        }
    }
}

[Mapper]
public partial class IdentityRoleDtoToAssignedRoleViewModelMapper : MapperBase<IdentityRoleDto, AssignedRoleViewModel>
{
    [MapperIgnoreTarget(nameof(AssignedRoleViewModel.IsAssigned))]
    public override partial AssignedRoleViewModel Map(IdentityRoleDto source);

    [MapperIgnoreTarget(nameof(AssignedRoleViewModel.IsAssigned))]
    public override partial void Map(IdentityRoleDto source, AssignedRoleViewModel destination);
}

[Mapper]
public partial class IdentityUserDtoToDetailViewModelMapper : MapperBase<IdentityUserDto, DetailViewModel>
{
    [MapperIgnoreTarget(nameof(DetailViewModel.CreatedBy))]
    [MapperIgnoreTarget(nameof(DetailViewModel.ModifiedBy))]
    public override partial DetailViewModel Map(IdentityUserDto source);

    [MapperIgnoreTarget(nameof(DetailViewModel.CreatedBy))]
    [MapperIgnoreTarget(nameof(DetailViewModel.ModifiedBy))]
    public override partial void Map(IdentityUserDto source, DetailViewModel destination);
}

[Mapper]
public partial class IdentityUserDtoToUserInfoViewModelMapper : MapperBase<IdentityUserDto, UserInfoViewModel>
{
    public override partial UserInfoViewModel Map(IdentityUserDto source);

    public override partial void Map(IdentityUserDto source, UserInfoViewModel destination);
}

[Mapper]
public partial class IdentityRoleDtoToEditRoleInfoMapper : MapperBase<IdentityRoleDto, EditRoleModal.RoleInfoModel>
{
    public override partial EditRoleModal.RoleInfoModel Map(IdentityRoleDto source);

    public override partial void Map(IdentityRoleDto source, EditRoleModal.RoleInfoModel destination);
}

public class UserInfoViewModelToIdentityUserUpdateDtoMapper : MapperBase<UserInfoViewModel, IdentityUserUpdateDto>
{
    public override IdentityUserUpdateDto Map(UserInfoViewModel source)
    {
        var destination = new IdentityUserUpdateDto();
        Map(source, destination);
        return destination;
    }

    public override void Map(UserInfoViewModel source, IdentityUserUpdateDto destination)
    {
        destination.UserName = source.UserName;
        destination.Name = source.Name;
        destination.Surname = source.Surname;
        destination.Email = source.Email;
        destination.ConcurrencyStamp = source.ConcurrencyStamp;
        IdentityExtraPropertiesCopier.Copy(source, destination);
    }
}

public class CreateRoleInfoToIdentityRoleCreateDtoMapper : MapperBase<CreateRoleModal.RoleInfoModel, IdentityRoleCreateDto>
{
    public override IdentityRoleCreateDto Map(CreateRoleModal.RoleInfoModel source)
    {
        var destination = new IdentityRoleCreateDto
        {
            Name = source.Name,
            IsDefault = source.IsDefault,
            IsPublic = source.IsPublic,
        };
        IdentityExtraPropertiesCopier.Copy(source, destination);
        return destination;
    }

    public override void Map(CreateRoleModal.RoleInfoModel source, IdentityRoleCreateDto destination)
    {
        destination.Name = source.Name;
        destination.IsDefault = source.IsDefault;
        destination.IsPublic = source.IsPublic;
        IdentityExtraPropertiesCopier.Copy(source, destination);
    }
}

public class EditRoleInfoToIdentityRoleUpdateDtoMapper : MapperBase<EditRoleModal.RoleInfoModel, IdentityRoleUpdateDto>
{
    public override IdentityRoleUpdateDto Map(EditRoleModal.RoleInfoModel source)
    {
        var destination = new IdentityRoleUpdateDto
        {
            Name = source.Name,
            IsDefault = source.IsDefault,
            IsPublic = source.IsPublic,
            ConcurrencyStamp = source.ConcurrencyStamp,
        };
        IdentityExtraPropertiesCopier.Copy(source, destination);
        return destination;
    }

    public override void Map(EditRoleModal.RoleInfoModel source, IdentityRoleUpdateDto destination)
    {
        destination.Name = source.Name;
        destination.IsDefault = source.IsDefault;
        destination.IsPublic = source.IsPublic;
        destination.ConcurrencyStamp = source.ConcurrencyStamp;
        IdentityExtraPropertiesCopier.Copy(source, destination);
    }
}
