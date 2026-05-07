using AutoMapper;
using Unity.GrantManager.ApplicantProfile;
using Unity.GrantManager.ApplicantProfile.ProfileData;
using Unity.GrantManager.Web.Pages.ApplicantContact;

namespace Unity.GrantManager.Web;

public class GrantManagerWebAutoMapperProfile : Profile
{
    public GrantManagerWebAutoMapperProfile()
    {
        CreateMap<ContactInfoItemDto, ApplicantContactModalViewModel>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.ContactId));

        CreateMap<ApplicantContactModalViewModel, UpdateApplicantContactDto>();
    }
}
