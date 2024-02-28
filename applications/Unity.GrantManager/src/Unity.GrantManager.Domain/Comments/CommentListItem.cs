using System;

namespace Unity.GrantManager.Comments
{
    public class CommentListItem
    {
        public string Comment { get; set; } = string.Empty;
        public string CommenterBadge { get; set; } = string.Empty;
        public string CommenterDisplayName { get; set; } = string.Empty;
        public Guid CommenterId { get; set; }
        public DateTime CreationTime { get; set; }
        public Guid OwnerId { get; set; }     
        public Guid Id { get; set; }
        public DateTime? LastModificationTime { get; set; }

}
}
