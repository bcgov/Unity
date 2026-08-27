namespace Unity.GrantManager.ApplicantProfile.ProfileData;

/// <summary>
/// Represents a link to be used within the Applicant Portal, including the URL, title, and description.
/// </summary>
public class ExternalLinkDto
{
    public required string Uri { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Order { get; set; } = -1;
}
