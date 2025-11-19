using AutoMapper;
using System.Text.Json;
using Unity.Reporting.Configuration;
using Unity.Reporting.Domain.Configuration;
using Unity.Reporting.TenantViewRole;

namespace Unity.Reporting;

/// <summary>
/// AutoMapper configuration profile for Unity.Reporting module defining mappings between domain entities and DTOs.
/// Handles complex object transformations including JSON deserialization of mapping configuration data
/// and proper conversion between ReportColumnsMap entities and their corresponding data transfer objects.
/// </summary>
public class ReportingApplicationAutoMapperProfile : Profile
{
    public ReportingApplicationAutoMapperProfile()
    {
        // Entity to DTO (Read operations)
        CreateMap<ReportColumnsMap, ReportColumnsMapDto>()
            .ForMember(dest => dest.Mapping, opt => opt.ConvertUsing<MappingJsonConverter, string>(src => src.Mapping))
            .ForMember(dest => dest.DetectedChanges, opt => opt.Ignore());

        CreateMap<Mapping, MappingDto>();  
        CreateMap<MapRow, MapRowDto>();

        // Tenant view role mappings (if needed for future use)
        CreateMap<UpdateTenantViewRoleDto, TenantViewRoleDto>()
            .ForMember(dest => dest.TenantId, opt => opt.Ignore())
            .ForMember(dest => dest.TenantName, opt => opt.Ignore())
            .ForMember(dest => dest.IsAssigned, opt => opt.Ignore());
    }
    
    /// <summary>
    /// Custom AutoMapper value converter for deserializing JSON mapping strings into MappingDto objects.
    /// Provides safe JSON deserialization with error handling and fallback to empty mapping objects
    /// when source data is invalid or malformed.
    /// </summary>
    private sealed class MappingJsonConverter : IValueConverter<string, MappingDto>
    {
        public MappingDto Convert(string sourceMember, ResolutionContext context)
        {
            if (string.IsNullOrWhiteSpace(sourceMember))
            {
                return new MappingDto();
            }
            
            try
            {
                return JsonSerializer.Deserialize<MappingDto>(sourceMember, (JsonSerializerOptions?)null) ?? new MappingDto();
            }
            catch
            {
                return new MappingDto();
            }
        }
    }
}
