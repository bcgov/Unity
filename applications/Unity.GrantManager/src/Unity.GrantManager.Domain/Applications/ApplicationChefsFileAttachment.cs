using System;
using System.ComponentModel.DataAnnotations;
using Unity.GrantManager.Attachments;

namespace Unity.GrantManager.Applications;

public class ApplicationChefsFileAttachment : AbstractAttachmentBase
{
    public override AttachmentType AttachmentType => AttachmentType.CHEFS;
    public Guid ApplicationId { get; set; }
    public string? ChefsSumbissionId { get; set; }
    public string? ChefsFileId { get; set; }
    
    [MaxLength(2048)]
    public string? AISummary { get; set; }
}
