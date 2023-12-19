using System;
using Volo.Abp.Application.Dtos;

namespace Unity.GrantManager.Locality;

[Serializable]
public class EconomicRegionDto : EntityDto<Guid>
{
    public string EconomicRegionName { get; set; } = string.Empty;

    public string EconomicRegionCode { get; set; } = string.Empty;

}
