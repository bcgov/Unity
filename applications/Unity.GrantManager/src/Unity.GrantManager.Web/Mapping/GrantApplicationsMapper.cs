using AutoMapper;
using Unity.GrantManager.Applications;
using Unity.GrantManager.GrantApplications;
using Unity.GrantManager.Web.Views.Shared.Components.SummaryWidget;

namespace Unity.GrantManager.Web.Mapping
{
    public class GrantApplicationsMapper : Profile
    {
        public GrantApplicationsMapper()
        {
            CreateMap<Application, GrantApplicationDto>();
            CreateMap<GetSummaryDto, SummaryWidgetViewModel>();
        }
    }
}
