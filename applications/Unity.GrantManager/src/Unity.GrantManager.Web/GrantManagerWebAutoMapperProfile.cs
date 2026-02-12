using AutoMapper;
using Unity.TenantManagement;
using Unity.TenantManagement.Web.Pages.TenantManagement.Tenants;

namespace Unity.GrantManager.Web
{
    public class GrantManagerWebAutoMapperProfile : Profile
    {
        public GrantManagerWebAutoMapperProfile()
        {
            // Web-specific mappings only
            CreateMap<TenantDto, EditModalModel.TenantInfoModel>()
                .ForMember(dest => dest.CasClientCode, opt => opt.MapFrom(src => src.CasClientCode))
                .ForMember(dest => dest.Division, opt => opt.MapFrom(src => src.Division))
                .ForMember(dest => dest.Branch, opt => opt.MapFrom(src => src.Branch))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description));

            CreateMap<EditModalModel.TenantInfoModel, TenantUpdateDto>()
                .ForMember(dest => dest.CasClientCode, opt => opt.MapFrom(src => src.CasClientCode))
                .ForMember(dest => dest.Division, opt => opt.MapFrom(src => src.Division))
                .ForMember(dest => dest.Branch, opt => opt.MapFrom(src => src.Branch))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description));

            CreateMap<CreateModalModel.TenantInfoModel, TenantCreateDto>()
                .ForMember(dest => dest.CasClientCode, opt => opt.MapFrom(src => src.CasClientCode))    
                .ForMember(dest => dest.Division, opt => opt.MapFrom(src => src.Division))
                .ForMember(dest => dest.Branch, opt => opt.MapFrom(src => src.Branch))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description));
        }
    }
}