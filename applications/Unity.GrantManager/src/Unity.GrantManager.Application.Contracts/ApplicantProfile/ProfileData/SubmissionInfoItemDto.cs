using System;
using System.Collections.Generic;

namespace Unity.GrantManager.ApplicantProfile.ProfileData;

public class SubmissionInfoItemDto
{
    public Guid Id { get; set; }
    public string LinkId { get; set; } = string.Empty;
    public DateTime ReceivedTime { get; set; }
    public DateTime SubmissionTime { get; set; }
    public string ReferenceNo { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public ExternalLinkDto? RenewalLink { get; set; }
    public List<ExternalLinkDto> RelatedLinks { get; set; } = [];
    public bool EligibleForRenewal { get; set; }
    public string ApplicantMessage { get; set; } = string.Empty;
}
