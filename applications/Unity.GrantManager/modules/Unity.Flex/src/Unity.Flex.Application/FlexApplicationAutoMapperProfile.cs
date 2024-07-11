using AutoMapper;
using System.Linq;
using Unity.Flex.Domain.ScoresheetInstances;
using Unity.Flex.Domain.Scoresheets;
using Unity.Flex.Domain.WorksheetInstances;
using Unity.Flex.Domain.WorksheetLinks;
using Unity.Flex.Domain.Worksheets;
using Unity.Flex.Scoresheets;
using Unity.Flex.WorksheetInstances;
using Unity.Flex.WorksheetLinks;
using Unity.Flex.Worksheets;

namespace Unity.Flex;

public class FlexApplicationAutoMapperProfile : Profile
{
    public FlexApplicationAutoMapperProfile()
    {
        CreateMap<Worksheet, WorksheetDto>()
            .ForMember(dest => dest.TotalSections, opt => opt.MapFrom(s => s.Sections.Select(s => s.Id).Count()))
            .ForMember(dest => dest.TotalFields, opt => opt.MapFrom(s => s.Sections.SelectMany(s => s.Fields).Count()));

        CreateMap<WorksheetLink, WorksheetLinkDto>();
        CreateMap<WorksheetSection, WorksheetSectionDto>();
        CreateMap<WorksheetInstance, WorksheetInstanceDto>();
        CreateMap<CustomFieldValue, CustomFieldValueDto>();
        CreateMap<CustomField, CustomFieldDto>();

        CreateMap<Worksheet, WorksheetBasicDto>()
            .ForMember(dest => dest.TotalSections, opt => opt.MapFrom(s => s.Sections.Select(s => s.Id).Count()))
            .ForMember(dest => dest.TotalFields, opt => opt.MapFrom(s => s.Sections.SelectMany(s => s.Fields).Count()));

        CreateMap<PersistWorksheetIntanceValuesDto, PersistWorksheetIntanceValuesEto>();        

        CreateMap<Question, QuestionDto>()
            .ForMember(dest => dest.ExtraProperties, opt => opt.Ignore())
            .ForMember(dest => dest.Answer, opt => opt.Ignore());
        CreateMap<ScoresheetSection, ScoresheetSectionDto>()
            .ForMember(dest => dest.Fields, opt => opt.MapFrom(src => src.Fields))
            .ForMember(dest => dest.ExtraProperties, opt => opt.Ignore());
        CreateMap<Scoresheet, ScoresheetDto>()
            .ForMember(dest => dest.Sections, opt => opt.MapFrom(src => src.Sections));
        CreateMap<ScoresheetInstance, ScoresheetInstanceDto>();
        CreateMap<Answer, AnswerDto>();
    }
}
