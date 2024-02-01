using System;
using Volo.Abp.Application.Dtos;

namespace Unity.GrantManager.Locality;

[Serializable]
public class RegionalDistrictDto : EntityDto<Guid>
{
    public string RegionalDistrictName { get; set; } = string.Empty;

    public string RegionalDistrictCode { get; set; } = string.Empty;

    public string EconomicRegionCode { get; set; } = string.Empty;

}
