namespace Unity.Notifications.Emails;

public class EmailCapabilitiesDto
{
    public bool CanView { get; set; }
    public bool CanCreate { get; set; }
    public bool CanEdit { get; set; }
    public bool CanSend { get; set; }
    public bool CanSchedule { get; set; }
    public bool CanDeleteDraft { get; set; }
    public bool CanCancelScheduled { get; set; }
}
