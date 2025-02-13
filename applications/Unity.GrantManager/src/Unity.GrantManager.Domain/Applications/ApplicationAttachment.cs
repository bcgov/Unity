using System;
using System.ComponentModel.DataAnnotations.Schema;
using Unity.GrantManager.Attachments;

namespace Unity.GrantManager.Applications;

public class ApplicationAttachment : AbstractS3Attachment
{
    [NotMapped]
    public override AttachmentType AttachmentType => AttachmentType.Application;
    public Guid ApplicationId { get; set; }

}
