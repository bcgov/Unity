using System;
using Volo.Abp.Application.Dtos;

namespace Unity.GrantManager.GrantApplications;

[Serializable]
public class ApplicantPortalStatusDto : EntityDto<Guid>
{
    public string ExternalStatus { get; set; } = string.Empty;

    public string InternalStatus { get; set; } = string.Empty;

    public string? NotifiedStatus { get; set; }

    public string StatusCode { get; set; } = string.Empty;
}
