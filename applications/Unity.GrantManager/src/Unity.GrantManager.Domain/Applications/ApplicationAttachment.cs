using System;
using Unity.GrantManager.Attachments;

namespace Unity.GrantManager.Applications;

public class ApplicationAttachment : AbstractS3Attachment
{
    public override AttachmentType AttachmentType => AttachmentType.APPLICATION;
    public Guid ApplicationId { get; set; }

}
