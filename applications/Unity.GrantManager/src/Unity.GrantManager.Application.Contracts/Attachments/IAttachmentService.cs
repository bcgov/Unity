using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.GrantApplications;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.Applications
{
    public interface IAttachmentService : IApplicationService
    {        
        Task<IList<ApplicationAttachmentDto>> GetApplicationAsync(Guid applicationId);
        Task<IList<AssessmentAttachmentDto>> GetAssessmentAsync(Guid assessmentId);
        Task ResyncSubmissionAttachmentsAsync(Guid applicationId);
    }
}
