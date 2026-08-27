using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Unity.GrantManager.ApplicantProfile;

/// <summary>
/// Represents the Applicant Portal external links configuration for an application form,
/// including the message shown to applicants alongside the renewal link.
/// </summary>
public class ExternalLinksConfig
{
    [MaxLength(512)]
    public string ApplicantMessage { get; set; } = string.Empty;

    public List<ExternalLink> Links { get; set; } = [];
}
