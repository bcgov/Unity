using AutoMapper;
using Unity.GrantManager.GrantPrograms;

namespace Unity.GrantManager.Web;

public class GrantManagerWebAutoMapperProfile : Profile
{
    public GrantManagerWebAutoMapperProfile()
    {
        //Define your AutoMapper configuration here for the Web project.
        CreateMap<GrantProgram, GrantProgramDto>();
        CreateMap<CreateUpdateGrantProgramDto, GrantProgram>();
    }
}
