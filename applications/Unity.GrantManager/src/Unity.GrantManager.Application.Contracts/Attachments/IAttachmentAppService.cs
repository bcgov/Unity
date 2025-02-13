﻿using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.Attachments;

public interface IAttachmentAppService : IApplicationService
{        
    Task<IList<ApplicationAttachmentDto>> GetApplicationAsync(Guid applicationId);
    Task<IList<AssessmentAttachmentDto>> GetAssessmentAsync(Guid assessmentId);
    Task ResyncSubmissionAttachmentsAsync(Guid applicationId);
    Task<IList<UnityAttachmentDto>> GetAttachmentsAsync(AttachmentType attachmentType, Guid attachedResourceId);
    Task<AttachmentMetadataDto> GetAttachmentMetadataAsync(AttachmentType attachmentType, Guid attachmentId);
    Task<AttachmentMetadataDto> UpdateAttachmentMetadataAsync(UpdateAttachmentMetadataDto updateAttachment);
}
