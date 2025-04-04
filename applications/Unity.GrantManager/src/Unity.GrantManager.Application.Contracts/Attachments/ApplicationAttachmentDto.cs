using System;
using Volo.Abp.Application.Dtos;

namespace Unity.GrantManager.Attachments;

public class ApplicationAttachmentDto : EntityDto<Guid>
{
    public string? FileName { get; set; }
    public string? DisplayName { get; set; }
    public string? AttachedBy { get; set; }
    public DateTime Time { get; set; }
    public string S3ObjectKey { get; set; } = string.Empty;
    public Guid? CreatorId { get; set; }
}
