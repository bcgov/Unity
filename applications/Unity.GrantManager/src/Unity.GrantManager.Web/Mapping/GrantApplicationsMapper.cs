using AutoMapper;
using Unity.GrantManager.GrantApplications;
using Unity.GrantManager.GrantPrograms;

namespace Unity.GrantManager.Web.Mapping
{
    public class GrantApplicationsMapper : Profile
    {
        public GrantApplicationsMapper()
        {
            CreateMap<GrantApplication, GrantApplicationDto>();
            CreateMap<CreateUpdateGrantProgramDto, GrantApplication>();
        }
    }
}
