namespace Unity.GrantManager.ApplicantPortal;

public class UpdateMultipleIdentitiesMessageDto
{
    public bool UseDefaultMessage { get; set; } = true;
    public string MessageHtml { get; set; } = string.Empty;
    // In default mode, null preserves the stored custom message. A supplied
    // template saves the retained custom draft without making it effective.
    public string? CustomMessageHtml { get; set; }
}

public class MultipleIdentitiesMessageDto : UpdateMultipleIdentitiesMessageDto
{
    public string DefaultMessageHtml { get; set; } = MultipleIdentitiesMessageDefaults.MessageHtml;
    public int MaxHtmlLength { get; set; } = MultipleIdentitiesMessageDefaults.MaxHtmlLength;
}
