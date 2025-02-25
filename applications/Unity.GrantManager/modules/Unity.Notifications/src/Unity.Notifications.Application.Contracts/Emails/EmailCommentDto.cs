﻿using System;
using System.Collections.Generic;

namespace Unity.Notifications.Emails;

[Serializable]
public class EmailCommentDto
{
    public string Subject { get; set; } = string.Empty;
    public string From { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public List<string> MentionNamesEmail { get; set; } = [];
}