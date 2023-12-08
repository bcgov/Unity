using System;
using Volo.Abp.Application.Dtos;

namespace Unity.GrantManager.Locality;

[Serializable]
public class ElectoralDistrictDto : EntityDto<Guid>
{
    public string ElectoralDistrictName { get; set; } = string.Empty;

    public string ElectoralDistrictCode { get; set; } = string.Empty;

}
