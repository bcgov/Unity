using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Assessments;
using Unity.GrantManager.Comments;
using Unity.GrantManager.GrantApplications;

namespace Unity.GrantManager;

public class GrantManagerApplicationAutoMapperProfile : Profile
{
    public GrantManagerApplicationAutoMapperProfile()
    {
        /* You can configure your AutoMapper mapping configuration here.
         * Alternatively, you can split your mapping configurations
         * into multiple profile classes for a better organization. */

        CreateMap<Application, GrantApplicationDto>();
        CreateMap<ApplicationUserAssignment, GrantApplicationAssigneeDto>();
        CreateMap<ApplicationStatus, ApplicationStatusDto>();
        CreateMap<AssessmentComment, CommentDto>()
            .ForMember(dest => dest.OwnerId, opt => opt.MapFrom(src => src.AssessmentId));
        CreateMap<ApplicationComment, CommentDto>()
            .ForMember(dest => dest.OwnerId, opt => opt.MapFrom(src => src.ApplicationId));
        CreateMap<Assessment, AssessmentDto>();
        CreateMap<ApplicationAttachment, ApplicationAttachmentDto>();
    }
}

