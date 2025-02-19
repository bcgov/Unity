using System;
using Volo.Abp.Application.Dtos;

namespace Unity.GrantManager.Attachments;

public class AttachmentMetadataDto : EntityDto<Guid>
{
    public AttachmentType AttachmentType { get; set; }
    public string? FileName { get; set; }
    public string? DisplayName { get; set; }
    public Guid? CreatorId { get; set; }
}