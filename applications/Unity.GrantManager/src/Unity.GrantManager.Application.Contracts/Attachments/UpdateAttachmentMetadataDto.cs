using System;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace Unity.GrantManager.Attachments;
public class UpdateAttachmentMetadataDto : EntityDto<Guid>
{
    public AttachmentType AttachmentType { get; set; }

    [MaxLength(256)]
    public string? DisplayName { get; set; }
}
