using AutoMapper;
using Volo.Abp.AutoMapper;
using Unity.Identity.Web.Pages.Identity.Roles;
using CreateUserModalModel = Unity.Identity.Web.Pages.Identity.Users.CreateModalModel;
using EditUserModalModel = Unity.Identity.Web.Pages.Identity.Users.EditModalModel;
using Unity.Identity.Web.Pages.Identity.Users;

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
        //List
        CreateMap<IdentityUserDto, EditUserModalModel.UserInfoViewModel>()
            .Ignore(x => x.Password);

        //CreateModal
        CreateMap<CreateUserModalModel.UserInfoViewModel, IdentityUserCreateDto>()
            .MapExtraProperties()
            .ForMember(dest => dest.RoleNames, opt => opt.Ignore());

        CreateMap<IdentityRoleDto, CreateUserModalModel.AssignedRoleViewModel>()
            .ForMember(dest => dest.IsAssigned, opt => opt.Ignore());

        //ImportModal
        CreateMap<IdentityRoleDto, ImportModalModel.AssignedRoleViewModel>()
            .ForMember(dest => dest.IsAssigned, opt => opt.Ignore());

        //EditModal
        CreateMap<EditUserModalModel.UserInfoViewModel, IdentityUserUpdateDto>()
            .MapExtraProperties()
            .ForMember(dest => dest.RoleNames, opt => opt.Ignore());

        CreateMap<IdentityRoleDto, EditUserModalModel.AssignedRoleViewModel>()
            .ForMember(dest => dest.IsAssigned, opt => opt.Ignore());

        CreateMap<IdentityUserDto, EditUserModalModel.DetailViewModel>()
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore());
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
