using AutoMapper;
using Unity.GrantManager.Applications;
using Unity.GrantManager.GrantApplications;
using Unity.GrantManager.Web.Pages.ApplicationContact;
using Unity.GrantManager.Web.Views.Shared.Components.ApplicantInfo;
using Unity.GrantManager.Web.Views.Shared.Components.SummaryWidget;

namespace Unity.GrantManager.Web.Mapping;

public class GrantApplicationsMapper : Profile
{
    public GrantApplicationsMapper()
    {
        CreateMap<Application, GrantApplicationDto>();
        CreateMap<GetSummaryDto, SummaryWidgetViewModel>().
            ForMember(dest => dest.SubmissionDate, opt => opt.MapFrom(s => s.SubmissionDate == null ? "" : s.SubmissionDate.Value.ToShortDateString()));
        CreateMap<ContactModalViewModel, ApplicationContactDto>();
        CreateMap<ApplicationContactDto, ContactModalViewModel>();

        CreateMap<ApplicantSummaryDto, ApplicantSummaryViewModel>();
        CreateMap<ContactInfoDto, ContactInfoViewModel>();
        CreateMap<SigningAuthorityDto, SigningAuthorityViewModel>();
        CreateMap<ApplicantAddressDto, ApplicantAddressViewModel>();
    }
}
