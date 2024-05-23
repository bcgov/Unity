using AutoMapper;
using Unity.Flex.Domain.Scoresheets;
using Unity.Flex.Domain.WorksheetInstances;
using Unity.Flex.Domain.Worksheets;
using Unity.Flex.Scoresheets;
using Unity.Flex.WorksheetInstances;
using Unity.Flex.Worksheets;

namespace Unity.Flex;

public class FlexApplicationAutoMapperProfile : Profile
{
    public FlexApplicationAutoMapperProfile()
    {
        CreateMap<Worksheet, WorksheetDto>();
        CreateMap<WorksheetSection, WorksheetSectionDto>();
        CreateMap<WorksheetInstance, WorksheetInstanceDto>();
        CreateMap<CustomFieldValue, CustomFieldValueDto>();
        CreateMap<CustomField, CustomFieldDto>();
        
        CreateMap<Question, QuestionDto>()
            .ForMember(dest => dest.ExtraProperties, opt => opt.Ignore());
        CreateMap<ScoresheetSection, ScoresheetSectionDto>()
            .ForMember(dest => dest.Fields, opt => opt.MapFrom(src => src.Fields))
            .ForMember(dest => dest.ExtraProperties, opt => opt.Ignore()); 
        CreateMap<Scoresheet, ScoresheetDto>()
            .ForMember(dest => dest.Sections, opt => opt.MapFrom(src => src.Sections))
            .ForMember(dest => dest.GroupVersions, opt => opt.Ignore());
    }
}
