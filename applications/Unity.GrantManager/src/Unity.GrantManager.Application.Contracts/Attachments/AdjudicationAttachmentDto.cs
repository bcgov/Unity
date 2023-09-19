using System;
using Volo.Abp.Application.Dtos;

namespace Unity.GrantManager.GrantApplications
{
    public class AdjudicationAttachmentDto : EntityDto<Guid>
    {
        public string? FileName { get; set; }
        public string? AttachedBy { get; set; }
        public Guid S3Guid { get; set; }
        public DateTime Time { get; set; }
        public Guid? CreatorId { get; set; }
    }
}
