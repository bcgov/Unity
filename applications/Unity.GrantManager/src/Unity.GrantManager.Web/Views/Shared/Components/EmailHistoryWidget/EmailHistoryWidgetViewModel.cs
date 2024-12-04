using System;

namespace Unity.GrantManager.Web.Views.Shared.Components.EmailHistoryWidget;

public class EmailHistoryWidgetViewModel
{

    public string Subject { get; set; } = string.Empty;
    public DateTime? SentDateTime { get; set; }
    public string SentBy { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}