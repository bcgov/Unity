using System;
using Volo.Abp.Application.Dtos;

namespace Unity.GrantManager.GrantApplications;

[Serializable]
public class ApplicationStatusDto : EntityDto<Guid>
{
    public string ExternalStatus { get; set; } = string.Empty;

    public string InternalStatus { get; set; } = string.Empty;

    public string StatusCode { get; set; } = string.Empty;
}
