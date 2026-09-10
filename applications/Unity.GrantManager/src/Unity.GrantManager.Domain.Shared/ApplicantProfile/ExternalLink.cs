using System.ComponentModel.DataAnnotations;

namespace Unity.GrantManager.ApplicantProfile;

/// <summary>
/// Represents a link to be used within the Applicant Portal, including the URI, title, and description.
/// </summary>
public class ExternalLink
{
    [MaxLength(2048)]
    public required string Uri { get; set; }

    [MaxLength(255)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(512)]
    public string Description { get; set; } = string.Empty;

    public ExternalLinkType ExternalLinkType { get; set; } = ExternalLinkType.Related;
    public bool Published { get; set; } = false;
    public int Order { get; set; } = -1;
}
