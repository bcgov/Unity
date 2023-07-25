using AutoMapper;
using Unity.GrantManager.GrantPrograms;

namespace Unity.GrantManager.Web.Mapping
{
    public class GrantProgramsMapper : Profile
    {
        public GrantProgramsMapper()
        {
            CreateMap<GrantProgram, GrantProgramDto>();
            CreateMap<CreateUpdateGrantProgramDto, GrantProgram>();
            CreateMap<GrantProgramDto, CreateUpdateGrantProgramDto>();
        }
    }
}

