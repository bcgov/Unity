namespace Unity.Notifications.EmailNotifications
{
    /// <summary>
    /// Groups common email content fields to reduce method parameter count (S107).
    /// </summary>
    public sealed record EmailMessageParams(
        string EmailTo,
        string Body,
        string Subject,
        string? EmailFrom,
        string? EmailTemplateName,
        string? EmailCC = null,
        string? EmailBCC = null,
        System.DateTime? SendOnDateTime = null);
}
