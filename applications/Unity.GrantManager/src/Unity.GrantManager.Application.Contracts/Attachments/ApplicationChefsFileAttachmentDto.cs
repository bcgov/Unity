using System;
using Volo.Abp.Application.Dtos;

namespace Unity.GrantManager.GrantApplications
{
    public class ApplicationChefsFileAttachmentDto : EntityDto<Guid>
    {
        public Guid ApplicationId { get; set; }
        public string ChefsSumbissionId { get; set; }
        public string ChefsFileId { get; set; }
        public string? Name { get; set; }
    }
}
