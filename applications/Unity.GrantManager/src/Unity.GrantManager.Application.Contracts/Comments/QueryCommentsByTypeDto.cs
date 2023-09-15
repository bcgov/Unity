using System;

namespace Unity.GrantManager.Comments
{
    public class QueryCommentsByTypeDto
    {
        public Guid OwnerId { get; set; }
        public CommentType CommentType { get; set; }
    }
}
