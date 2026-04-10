using System;
using System.Collections.Generic;
using Unity.Notifications.Comments;

namespace Unity.Notifications.Emails;

[Serializable]
public class EmailCommentDto
{
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string OwnerId { get; set; } = string.Empty;
    public CommentType CommentType { get; set; }
    public List<string> MentionNamesEmail { get; set; } = [];
    public string? EmailTemplateName { get; set; } = string.Empty;
}