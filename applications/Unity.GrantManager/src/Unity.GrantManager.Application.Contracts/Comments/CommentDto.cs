using System;
using Volo.Abp.Application.Dtos;

namespace Unity.GrantManager.Comments
{    
    public class CommentDto : EntityDto<Guid>
    {
        public string Comment { get; set; } = string.Empty;
    }
}
