using System;
using System.ComponentModel.DataAnnotations.Schema;
using Unity.GrantManager.Attachments;

namespace Unity.GrantManager.Applications;

public class ApplicationChefsFileAttachment : AbstractAttachmentBase
{
    public override AttachmentType AttachmentType => AttachmentType.CHEFS;
    public Guid ApplicationId { get; set; }
    public string? ChefsSumbissionId { get; set; }
    public string? ChefsFileId { get; set; }
}
