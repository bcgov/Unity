using System;
using Volo.Abp.Application.Dtos;

namespace Unity.Notifications.Emails;

[Serializable]
public class EmailLogAttachmentDto : EntityDto<Guid>
{
    public string? FileName { get; set; }
    public string? DisplayName { get; set; }
    public DateTime Time { get; set; }
    public long FileSize { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public string S3ObjectKey { get; set; } = string.Empty;
    public string AttachedBy { get; set; } = string.Empty;
}
