using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Unity.GrantManager.ApplicantProfile;

/// <summary>
/// Represents a link to be used within the Applicant Portal, including the URL, title, and description.
/// </summary>
[ComplexType]
public class ExternalLink
{
    /// <summary>
    /// Gets or sets the URL of the external link.
    /// </summary>
    [MaxLength(2048)]
    public required string Uri { get; set; }

    /// <summary>
    /// Gets or sets the type of the external link.
    /// </summary>
    public ExternalLinkType ExternalLinkType { get; set; } = ExternalLinkType.Related;
    public bool Publish { get; set; } = false;
    public int Order { get; set; } = -1;

    /// <summary>
    /// Gets or sets the title of the external link.
    /// </summary>
    [MaxLength(255)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the external link.
    /// </summary>
    [MaxLength(512)]
    public string Description { get; set; } = string.Empty;
}