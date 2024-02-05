using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Unity.GrantManager.Locality;

public class RegionalDistrict : AuditedAggregateRoot<Guid>
{
    public string RegionalDistrictName { get; set; } = string.Empty;

    public string RegionalDistrictCode { get; set; } = string.Empty;

    public string EconomicRegionCode { get; set; } = string.Empty;

}
