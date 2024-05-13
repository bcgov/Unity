using AutoMapper;
using Unity.Flex.Domain.WorksheetInstances;
using Unity.Flex.Domain.Worksheets;
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
    }
}
