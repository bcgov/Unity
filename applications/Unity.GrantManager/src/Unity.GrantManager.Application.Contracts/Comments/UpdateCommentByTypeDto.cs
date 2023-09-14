using System;

namespace Unity.GrantManager.Comments
{
    public class UpdateCommentByTypeDto : UpdateCommentDto
    {
        public Guid OwnerId { get; set; }
        public CommentType CommentType { get; set; }
    }
}
