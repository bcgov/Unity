using AutoMapper;
using Unity.GrantManager.ApplicationForms;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Assessments;
using Unity.GrantManager.Comments;
using Unity.GrantManager.Forms;
using Unity.GrantManager.GrantApplications;
using Unity.GrantManager.Intake;

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
        CreateMap<Assessment, AssessmentDto>()
            .ForMember(
                dest => dest.StartDate,
                opts => opts.MapFrom(src => src.CreationTime));
        CreateMap<AssessmentWithAssessorQueryResultItem, AssessmentListItemDto>();
        CreateMap<ApplicationAttachment, ApplicationAttachmentDto>();
        CreateMap<GrantPrograms.Intake, IntakeDto>();
        CreateMap<ApplicationForm, ApplicationFormDto>();
        CreateMap<CreateUpdateIntakeDto, GrantPrograms.Intake>();
        CreateMap<CreateUpdateApplicationFormDto, ApplicationForm>();
        CreateMap<AssessmentAttachment, AssessmentAttachmentDto>();
    }
}

