using System;
using Unity.GrantManager.Attachments;

namespace Unity.GrantManager.Applications;

public class ApplicationChefsFileAttachment : AbstractAttachmentBase
{
    public Guid ApplicationId { get; set; }
    public string? ChefsSumbissionId { get; set; }
    public string? ChefsFileId { get; set; }
}
