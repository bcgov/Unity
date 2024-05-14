using AutoMapper;
using Unity.Flex.Domain.Scoresheets;
using Unity.Flex.Scoresheets;

namespace Unity.Flex;

public class FlexApplicationAutoMapperProfile : Profile
{
    public FlexApplicationAutoMapperProfile()
    {
        CreateMap<Question, QuestionDto>()
            .ForMember(dest => dest.ExtraProperties, opt => opt.Ignore());
        CreateMap<ScoresheetSection, ScoresheetSectionDto>()
            .ForMember(dest => dest.Fields, opt => opt.MapFrom(src => src.Fields))
            .ForMember(dest => dest.ExtraProperties, opt => opt.Ignore()); 
        CreateMap<Scoresheet, ScoresheetDto>()
            .ForMember(dest => dest.Sections, opt => opt.MapFrom(src => src.Sections));

    }
}
