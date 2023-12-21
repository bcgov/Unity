using AutoMapper;
using static Unity.Identity.Web.Pages.Identity.Users.EditModalModel;
using EditUserModalModel = Unity.Identity.Web.Pages.Identity.Users.EditModalModel;

namespace Volo.Abp.Identity.Web;

public class UnityIdentityWebAutoMapperProfile : Profile
{
    public UnityIdentityWebAutoMapperProfile()
    {
        CreateUserMappings();
        CreateRoleMappings();
    }

    protected void CreateUserMappings()
    {
        //EditModal
        CreateMap<UserInfoViewModel, IdentityUserUpdateDto>()
            .MapExtraProperties()
            .ForMember(dest => dest.RoleNames, opt => opt.Ignore())
            .ForMember(dest => dest.PhoneNumber, opt => opt.Ignore())
            .ForMember(dest => dest.Password, opt => opt.Ignore())
            .ForMember(dest => dest.LockoutEnabled, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.Ignore());

        CreateMap<IdentityRoleDto, AssignedRoleViewModel>()
            .ForMember(dest => dest.IsAssigned, opt => opt.Ignore());

        CreateMap<IdentityUserDto, DetailViewModel>()
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore());

        CreateMap<IdentityUserDto, UserInfoViewModel>();
    }

    protected void CreateRoleMappings()
    {
        //List
        CreateMap<IdentityRoleDto, Unity.Identity.Web.Pages.Identity.Roles.EditModalModel.RoleInfoModel>();

        //CreateModal
        CreateMap<Unity.Identity.Web.Pages.Identity.Roles.CreateModalModel.RoleInfoModel, IdentityRoleCreateDto>()
            .MapExtraProperties();

        //EditModal
        CreateMap<Unity.Identity.Web.Pages.Identity.Roles.EditModalModel.RoleInfoModel, IdentityRoleUpdateDto>()
            .MapExtraProperties();
    }
}
