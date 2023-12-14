using System;
using Volo.Abp.Application.Dtos;

namespace Unity.GrantManager.Locality;

[Serializable]
public class CensusSubdivisionDto  : EntityDto<Guid>
{
    public string CensusSubdivisionName { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string RegionalDistrictCode { get; set; } = string.Empty;

}
