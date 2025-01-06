using System;

namespace Unity.GrantManager.Emails
{
    public class EmailListItem
    {
        public string Email { get; set; } = string.Empty;
        public DateTime CreationTime { get; set; }
        public Guid OwnerId { get; set; }
        public Guid Id { get; set; }
        public DateTime? LastModificationTime { get; set; }
    }
}
