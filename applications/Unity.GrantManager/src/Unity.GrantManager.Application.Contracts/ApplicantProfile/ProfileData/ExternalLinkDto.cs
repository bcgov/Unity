namespace Unity.GrantManager.ApplicantProfile.ProfileData;

/// <summary>
/// Represents a link to be used within the Applicant Portal, including the URL, title, and description.
/// </summary>
public class ExternalLinkDto
{
    /// <summary>
    /// Gets or sets the URL of the external link.
    /// </summary>
    public string Uri { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the title of the external link.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the external link.
    /// </summary>
    public string Description { get; set; } = string.Empty;
}
