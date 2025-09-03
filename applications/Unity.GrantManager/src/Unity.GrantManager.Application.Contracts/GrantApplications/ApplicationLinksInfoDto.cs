using System;
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

}
