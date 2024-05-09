using AutoMapper;
using Unity.Flex.Domain.Scoresheets;
using Unity.Flex.Scoresheets;

namespace Unity.Flex;

public class FlexApplicationAutoMapperProfile : Profile
{
    public FlexApplicationAutoMapperProfile()
    {
        CreateMap<Scoresheet, ScoresheetDto>();
    }
}
