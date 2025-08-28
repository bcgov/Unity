using System;
using Volo.Abp.Application.Dtos;

namespace Unity.GrantManager.Emails
{
    public class EmailDto : EntityDto<Guid>
    {
        public string Email { get; set; } = string.Empty;
        public DateTime CreationTime { get; set; }
        public Guid OwnerId { get; set; }
        public DateTime? LastModificationTime { get; set; }
    }
}
