using System;

namespace Unity.GrantManager.Attachments;

public class AttachmentParametersDto
{
    public AttachmentType AttachmentType { get; set; }
    public Guid AttachedResourceId { get; set; }

    public AttachmentParametersDto()
    {
        
    }

    public AttachmentParametersDto(AttachmentType attachmentType, Guid attachedResourceId)
    {
        AttachmentType = attachmentType;
        AttachedResourceId = attachedResourceId;
    }
}
