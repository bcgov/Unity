using AutoMapper;
using Unity.GrantManager.GrantApplications;
using Unity.GrantManager.GrantPrograms;
using Unity.GrantManager.Web.Views.Shared.Components.SummaryWidget;

namespace Unity.GrantManager.Web.Mapping
{
    public class GrantApplicationsMapper : Profile
    {
        public GrantApplicationsMapper()
        {
            CreateMap<GrantApplication, GrantApplicationDto>();
            CreateMap<CreateUpdateGrantProgramDto, GrantApplication>();
            CreateMap<GetSummaryDto, SummaryWidgetViewModel>();
        }
    }
}
