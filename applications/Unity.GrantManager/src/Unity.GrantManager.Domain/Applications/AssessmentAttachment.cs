using System;
using System.ComponentModel.DataAnnotations.Schema;
using Unity.GrantManager.Attachments;

namespace Unity.GrantManager.Applications;

public class AssessmentAttachment : AbstractS3Attachment
{
    [NotMapped]
    public override AttachmentType AttachmentType => AttachmentType.ASSESSMENT;
    public Guid AssessmentId { get; set; }
}

