using System;
using System.Collections.Generic;
using System.Linq;
using Unity.GrantManager.Applications;
using Volo.Abp.Application.Dtos;

namespace Unity.GrantManager.GrantApplications;

[Serializable]
public class ApplicationLinksInfoDto : EntityDto<Guid>
{
    public Guid ApplicationId { get; set; }
    public String ReferenceNumber { get; set; } = String.Empty;
    public String ApplicationStatus { get; set; } = String.Empty;
    public String Category { get; set; } = String.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public string ApplicantName { get; set; } = string.Empty;
    public ApplicationLinkType LinkType { get; set; } = ApplicationLinkType.Related;
    public int? FormVersion { get; set; }
}

[Serializable]
public class ApplicationLinkValidationRequest
{
    public Guid TargetApplicationId { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public ApplicationLinkType LinkType { get; set; }
}

[Serializable]
public class ApplicationLinkValidationResult
{
    public Dictionary<string, bool> ValidationErrors { get; set; } = new();
    public Dictionary<string, string> ErrorMessages { get; set; } = new();
    public bool HasErrors => ValidationErrors.Any(x => x.Value);
}
