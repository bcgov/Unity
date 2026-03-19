using System;
using Volo.Abp.Application.Dtos;

namespace Unity.GrantManager.Attachments;

public class ApplicationChefsFileAttachmentDto : EntityDto<Guid>
{
    public Guid ApplicationId { get; set; }
    public string ChefsSubmissionId { get; set; } = string.Empty;
    public string ChefsFileId { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? FileName { get; set; }
    public string? DisplayName { get; set; }
    public string? AISummary { get; set; }
    public DateTime CreationTime { get; set; }
    public DateTime? LastModificationTime { get; set; }
}
