using System;
using Unity.GrantManager.Attachments;

namespace Unity.GrantManager.Applications;

public class AssessmentAttachment : AbstractS3Attachment
{
    public Guid AssessmentId { get; set; }
}

