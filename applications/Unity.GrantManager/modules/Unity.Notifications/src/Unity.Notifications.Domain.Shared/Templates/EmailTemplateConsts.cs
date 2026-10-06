namespace Unity.Notifications.Templates;

/// <summary>
/// Shared limits for <c>EmailTemplate</c>, referenced by the EF Core mapping, the DTO validation, and the template editor UI.
/// </summary>
public static class EmailTemplateConsts
{
    /// <summary>
    /// Maximum number of characters allowed in an email template name.
    /// </summary>
    public const int MaxNameLength = 50;
}
