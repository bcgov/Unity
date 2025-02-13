using System;
using Unity.GrantManager.Attachments;

namespace Unity.GrantManager.Applications;

public class ApplicationAttachment : AbstractS3Attachment
{
    public Guid ApplicationId { get; set; }
}
