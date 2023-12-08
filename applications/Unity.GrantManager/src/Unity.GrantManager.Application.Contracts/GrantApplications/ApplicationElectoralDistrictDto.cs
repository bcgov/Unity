using System;
using Volo.Abp.Application.Dtos;

namespace Unity.GrantManager.GrantApplications;

[Serializable]
public class ApplicationElectoralDistrictDto : EntityDto<Guid>
{
    public string ElectoralDistrictName { get; set; } = string.Empty;

    public string ElectoralDistrictCode { get; set; } = string.Empty;

}
