using System;
using System.Collections.Generic;

namespace Unity.Notifications.Emails;

[Serializable]
public class EmailCommentDto
{
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string OwnerId { get; set; } = string.Empty;
    public int CommentType { get; set; }
    public List<string> MentionNamesEmail { get; set; } = [];
    public string? EmailTemplateName { get; set; } = string.Empty;
}