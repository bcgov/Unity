using System;

namespace Unity.GrantManager.GrantApplications;

[Serializable]
public class ApplicantPortalProgramDetailsDto
{
    public string DisplayName { get; set; } = string.Empty;

    public string Division { get; set; } = string.Empty;

    public string Branch { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
}
