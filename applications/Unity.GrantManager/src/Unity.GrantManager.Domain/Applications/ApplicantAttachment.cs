using System;
using System.ComponentModel.DataAnnotations.Schema;
using Unity.GrantManager.Attachments;

namespace Unity.GrantManager.Applications;

public class ApplicantAttachment : AbstractS3Attachment
{
    [NotMapped]
    public override AttachmentType AttachmentType => AttachmentType.APPLICANT;
    public Guid ApplicantId { get; set; }
}
