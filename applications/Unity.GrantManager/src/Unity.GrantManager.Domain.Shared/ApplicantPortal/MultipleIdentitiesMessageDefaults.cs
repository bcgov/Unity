namespace Unity.GrantManager.ApplicantPortal;

public static class MultipleIdentitiesMessageDefaults
{
    // ABP's Settings.Value column allows 2,048 characters, including HTML.
    public const int MaxHtmlLength = 2048;
    public const string EmailParameter = "{inboxEmail}";
    public const string MessageHtml =
        "<p>This page is displayed in read-only mode because multiple applicants are associated with your account.</p>" +
        "<p>To resolve this issue, please contact the grant program administrator at {inboxEmail}.</p>";
}
