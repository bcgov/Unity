namespace Unity.GrantManager.Web.Views.Shared.Components.EmailHistoryWidget;

public class EmailHistoryWidgetViewModel
{
    public System.Guid ApplicationId { get; set; }
    public System.Guid ApplicantId { get; set; }
    public string ApplicantName { get; set; } = string.Empty;
    public string UnityApplicantId { get; set; } = string.Empty;
    public bool EnableEmailDelay { get; set; }
    public Unity.Notifications.Emails.EmailCapabilitiesDto Capabilities { get; set; } = new();
}
