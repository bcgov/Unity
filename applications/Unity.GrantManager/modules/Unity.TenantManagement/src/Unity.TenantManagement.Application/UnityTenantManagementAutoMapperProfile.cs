#nullable enable

using AutoMapper;
using Volo.Abp.TenantManagement;

namespace Unity.TenantManagement
{
    public class UnityTenantManagementAutoMapperProfile : Profile
    {
        public UnityTenantManagementAutoMapperProfile()
        {
       // Add the tenant management mapping here
        CreateMap<Tenant, TenantDto>()
            .ForMember(dest => dest.CasClientCode, opt => opt.MapFrom(src => src.ExtraProperties.ContainsKey("CasClientCode") ? (string?)src.ExtraProperties["CasClientCode"] : null))
            .ForMember(dest => dest.Division, opt => opt.MapFrom(src => src.ExtraProperties.ContainsKey("Division") ? (string?)src.ExtraProperties["Division"] : null))
            .ForMember(dest => dest.Branch, opt => opt.MapFrom(src => src.ExtraProperties.ContainsKey("Branch") ? (string?)src.ExtraProperties["Branch"] : null))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.ExtraProperties.ContainsKey("Description") ? (string?)src.ExtraProperties["Description"] : null));
        }
    }
}