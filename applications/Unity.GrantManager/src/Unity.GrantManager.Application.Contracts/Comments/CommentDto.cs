using System;
using Volo.Abp.Application.Dtos;

namespace Unity.GrantManager.Comments
{
    public class CommentDto : EntityDto<Guid>
    {
        public string Comment { get; set; } = string.Empty;
        public string Commenter { get; set; } = string.Empty;
        public DateTime CreationTime { get; set; }
        public Guid OwnerId { get; set; }
        public Guid CreatorId { get; set; }
    }
}
