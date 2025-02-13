using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Unity.GrantManager.Attachments;

namespace Unity.GrantManager.Applications;

public interface IAttachmentService : IApplicationService
{        
    Task<IList<ApplicationAttachmentDto>> GetApplicationAsync(Guid applicationId);
    Task<IList<AssessmentAttachmentDto>> GetAssessmentAsync(Guid assessmentId);
    Task ResyncSubmissionAttachmentsAsync(Guid applicationId);
    Task<AttachmentMetadataDto> UpdateAttachmentMetadataAsync(UpdateAttachmentMetadataDto updateAttachment);
}
