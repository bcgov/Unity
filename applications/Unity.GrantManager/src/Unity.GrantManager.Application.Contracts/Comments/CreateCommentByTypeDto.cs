using System;

namespace Unity.GrantManager.Comments
{
    public class CreateCommentByTypeDto : CreateCommentDto
    {
        public Guid OwnerId { get; set; }
        public CommentType CommentType { get; set; }        
    }
}
