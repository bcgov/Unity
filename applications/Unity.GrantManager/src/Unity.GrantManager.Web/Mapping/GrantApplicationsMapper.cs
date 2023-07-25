using AutoMapper;
using Unity.GrantManager.GrantApplications;

namespace Unity.GrantManager.Web.Mapping
{
    public class GrantApplicationsMapper : Profile
    {
        public GrantApplicationsMapper()
        {
            CreateMap<GrantApplication, GrantApplicationDto>();
        }
    }
}
