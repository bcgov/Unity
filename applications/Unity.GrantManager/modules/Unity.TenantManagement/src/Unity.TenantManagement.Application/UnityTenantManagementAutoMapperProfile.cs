using AutoMapper;
using System.Collections.Generic;
using Volo.Abp.TenantManagement;

namespace Unity.TenantManagement
{
    public class UnityTenantManagementAutoMapperProfile : Profile
    {
        public UnityTenantManagementAutoMapperProfile()
        {
            CreateMap<Tenant, TenantDto>()
                .ForMember(dest => dest.CasClientCode, opt => opt.MapFrom(src => GetExtraPropertyAsString(src, "CasClientCode")))            
                .ForMember(dest => dest.Division, opt => opt.MapFrom(src => GetExtraPropertyAsString(src, "Division")))
                .ForMember(dest => dest.Branch, opt => opt.MapFrom(src => GetExtraPropertyAsString(src, "Branch")))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => GetExtraPropertyAsString(src, "Description")));

            CreateMap<TenantCreateDto, Tenant>()
                .ForMember(dest => dest.ExtraProperties, opt => opt.MapFrom(src => CreateExtraProperties(src)));

            CreateMap<TenantUpdateDto, Tenant>()
                .ForMember(dest => dest.ExtraProperties, opt => opt.MapFrom(src => CreateExtraProperties(src)));
        }

        private static string GetExtraPropertyAsString(Tenant tenant, string propertyName)
        {
            if (tenant.ExtraProperties?.ContainsKey(propertyName) == true)
            {
                return tenant.ExtraProperties[propertyName]?.ToString() ?? string.Empty;
            }
            return string.Empty;
        }

        private static Dictionary<string, object> CreateExtraProperties(TenantCreateOrUpdateDtoBase dto)
        {
            var extraProperties = new Dictionary<string, object>();
            
            if (dto is TenantCreateDto createDto)
            {
                extraProperties["Division"] = createDto.Division ?? string.Empty;
                extraProperties["Branch"] = createDto.Branch ?? string.Empty;
                extraProperties["Description"] = createDto.Description ?? string.Empty;
                extraProperties["CasClientCode"] = createDto.CasClientCode ?? string.Empty;
            }
            else if (dto is TenantUpdateDto updateDto)
            {
                extraProperties["Division"] = updateDto.Division ?? string.Empty;
                extraProperties["Branch"] = updateDto.Branch ?? string.Empty;
                extraProperties["Description"] = updateDto.Description ?? string.Empty;
                extraProperties["CasClientCode"] = updateDto.CasClientCode ?? string.Empty;
            }
                
            return extraProperties;
        }
    }
}