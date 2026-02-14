#nullable enable

using AutoMapper;
using Volo.Abp.TenantManagement;

namespace Unity.TenantManagement
{
    public class UnityTenantManagementAutoMapperProfile : Profile
    {
        public UnityTenantManagementAutoMapperProfile()
        {
            CreateMap<Tenant, TenantDto>()
                .ForMember(dest => dest.CasClientCode, opt => opt.MapFrom(src => 
                    GetExtraProperty(src, "CasClientCode")))
                .ForMember(dest => dest.Division, opt => opt.MapFrom(src => 
                    GetExtraProperty(src, "Division")))
                .ForMember(dest => dest.Branch, opt => opt.MapFrom(src => 
                    GetExtraProperty(src, "Branch")))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => 
                    GetExtraProperty(src, "Description")));
        }

        private static string? GetExtraProperty(Tenant tenant, string key)
        {
            return tenant.ExtraProperties.TryGetValue(key, out var value) ? value?.ToString() : null;
        }
    }
}