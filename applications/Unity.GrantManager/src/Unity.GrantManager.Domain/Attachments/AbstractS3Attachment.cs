using System;

namespace Unity.GrantManager.Attachments;

public abstract class AbstractS3Attachment : AbstractAttachmentBase
{
    public string S3ObjectKey { get; set; } = string.Empty;
    public DateTime Time { get; set; }
    public Guid UserId { get; set; }
}
