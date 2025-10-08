using AutoMapper;
using System.Text.Json;
using Unity.Reporting.Configuration;
using Unity.Reporting.Domain.Configuration;

namespace Unity.Reporting;

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
    }
    
    private class MappingJsonConverter : IValueConverter<string, MappingDto>
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
