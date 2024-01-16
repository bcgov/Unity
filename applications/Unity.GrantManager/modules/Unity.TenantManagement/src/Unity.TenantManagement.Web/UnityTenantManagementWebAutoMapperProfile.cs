using AutoMapper;
using Unity.TenantManagement.Web.Pages.TenantManagement.Tenants;
using static Unity.TenantManagement.Web.Pages.TenantManagement.Tenants.AssignManagerModalModel;

namespace Unity.TenantManagement.Web;

public class AbpTenantManagementWebAutoMapperProfile : Profile
{
    public AbpTenantManagementWebAutoMapperProfile()
    {
        //List
        CreateMap<TenantDto, EditModalModel.TenantInfoModel>()
            .MapExtraProperties();

        //CreateModal
        CreateMap<CreateModalModel.TenantInfoModel, TenantCreateDto>()            
            .MapExtraProperties();

        //EditModal
        CreateMap<EditModalModel.TenantInfoModel, TenantUpdateDto>()
            .MapExtraProperties();

        //AssignManagerModal
        CreateMap<TenantDto, AssignManagerInfoModel>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(s => s.Id))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(s => s.Name))
            .ForMember(dest => dest.FirstName, opt => opt.Ignore())
            .ForMember(dest => dest.LastName, opt => opt.Ignore())
            .ForMember(dest => dest.UserIdentifier, opt => opt.Ignore())
            .ForMember(dest => dest.Directory, opt => opt.Ignore())
            .MapExtraProperties();            
    }
}
