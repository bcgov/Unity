using AutoMapper;
using Unity.GrantManager.ApplicationForms;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Assessments;
using Unity.GrantManager.Attachments;
using Unity.GrantManager.Comments;
using Unity.GrantManager.Events;
using Unity.GrantManager.Forms;
using Unity.GrantManager.GrantApplications;
using Unity.GrantManager.Identity;
using Unity.GrantManager.Intakes;
using Unity.GrantManager.Locality;
using Unity.GrantManager.Zones;

namespace Unity.GrantManager;

public class GrantManagerApplicationAutoMapperProfile : Profile
{
    public GrantManagerApplicationAutoMapperProfile()
    {
        /* You can configure your AutoMapper mapping configuration here.
         * Alternatively, you can split your mapping configurations
         * into multiple profile classes for a better organization. */

        CreateMap<Application, GrantApplicationDto>();
        CreateMap<ApplicationAssignment, GrantApplicationAssigneeDto>();
        CreateMap<Person, GrantApplicationAssigneeDto>()
            .ForMember(d => d.AssigneeId, opt => opt.MapFrom(src => src.Id))
            .ForMember(d => d.FullName, opt => opt.MapFrom(src => src.FullName))
            .ForMember(d => d.ApplicationId, opt => opt.Ignore())
            .ForMember(d => d.Duty, opt => opt.Ignore());
        CreateMap<ApplicationStatus, ApplicationStatusDto>();
        CreateMap<AssessmentComment, CommentDto>()
            .ForMember(dest => dest.OwnerId, opt => opt.MapFrom(src => src.AssessmentId));
        CreateMap<ApplicationComment, CommentDto>()
            .ForMember(dest => dest.OwnerId, opt => opt.MapFrom(src => src.ApplicationId));
        CreateMap<CommentListItem, CommentDto>()
            .ForMember(dest => dest.Badge, opt => opt.MapFrom(src => src.CommenterBadge))
            .ForMember(dest => dest.Commenter, opt => opt.MapFrom(src => src.CommenterDisplayName))
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id));
        CreateMap<Assessment, AssessmentDto>()
            .ForMember(
                dest => dest.StartDate,
                opts => opts.MapFrom(src => src.CreationTime));
        CreateMap<AssessmentWithAssessorQueryResultItem, AssessmentListItemDto>()
            .ForMember(d => d.SubTotal, opt => opt.Ignore());
        CreateMap<ApplicationChefsFileAttachment, ApplicationChefsFileAttachmentDto>();
        CreateMap<ApplicationAttachment, ApplicationAttachmentDto>();
        CreateMap<Intakes.Intake, IntakeDto>();
        CreateMap<ApplicationForm, ApplicationFormDto>();
        CreateMap<ApplicationFormVersion, ApplicationFormVersionDto>();
        CreateMap<ApplicationFormVersionDto, ApplicationFormVersion>();
        CreateMap<CreateUpdateApplicationFormVersionDto, ApplicationFormVersion>();
        CreateMap<CreateUpdateIntakeDto, Intakes.Intake>();
        CreateMap<CreateUpdateApplicationFormDto, ApplicationForm>();
        CreateMap<AssessmentAttachment, AssessmentAttachmentDto>();
        CreateMap<ApplicationActionResultItem, ApplicationActionDto>();
        CreateMap<EventSubscription, EventSubscriptionDto>();
        CreateMap<EventSubscriptionDto, EventSubscription>();
        CreateMap<Sector, SectorDto>();
        CreateMap<SubSector, SubSectorDto>();
        CreateMap<EconomicRegion, EconomicRegionDto>();
        CreateMap<ElectoralDistrict, ElectoralDistrictDto>();
        CreateMap<Community, CommunityDto>();
        CreateMap<RegionalDistrict, RegionalDistrictDto>();
        CreateMap<ApplicationTags, ApplicationTagsDto>();
        CreateMap<Applicant, GrantApplicationApplicantDto>();
        CreateMap<ApplicationContact, ApplicationContactDto>();
        CreateMap<ApplicationContactDto, ApplicationContact>();
        CreateMap<ApplicationLink, ApplicationLinksDto>();
        CreateMap<ApplicationLinksDto, ApplicationLink>();
        CreateMap<Application, GrantApplicationLiteDto>();
        CreateMap<ApplicantAddress, ApplicantAddressDto>();
        CreateMap<ZoneGroupDefinition, ZoneGroupDefinitionDto>().ReverseMap();
        CreateMap<ZoneTabDefinition, ZoneTabDefinitionDto>().ReverseMap();
        CreateMap<ZoneDefinition, ZoneDefinitionDto>().ReverseMap();
    }
}

