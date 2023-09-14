using System;
using Volo.Abp.Application.Dtos;

namespace Unity.GrantManager.GrantApplications
{
    public class ApplicationAttachmentDto : EntityDto<Guid>
    {
        public string? FileName { get; set; }
        public DateTime Time { get; set; }
        public Guid? CreatorId { get; set; }
    }
}
