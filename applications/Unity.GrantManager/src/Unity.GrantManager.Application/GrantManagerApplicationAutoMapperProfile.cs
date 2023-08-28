using AutoMapper;
using System.Collections.Generic;
using Unity.GrantManager.Applications;
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
        CreateMap<AssessmentComment, AssessmentCommentDto>();
    }
}

