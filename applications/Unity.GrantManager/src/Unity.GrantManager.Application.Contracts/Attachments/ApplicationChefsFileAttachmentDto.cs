using System;
using Volo.Abp.Application.Dtos;

namespace Unity.GrantManager.Attachments;

public class ApplicationChefsFileAttachmentDto : EntityDto<Guid>
{
    public Guid ApplicationId { get; set; }
    public string ChefsSumbissionId { get; set; } = string.Empty;
    public string ChefsFileId { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? AISummary { get; set; }
}
