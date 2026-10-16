namespace Unity.GrantManager.Notifications
{
    /// <summary>
    /// Minimal view of a scheduled notification that references an email template,
    /// exposed to template editors without granting access to the schedule list.
    /// </summary>
    public class TemplateNotificationReferenceDto
    {
        public string TriggerType { get; set; } = string.Empty;
        public string? TriggerDetail { get; set; }
        public bool IsActive { get; set; }
    }
}
