using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Unity.GrantManager.Locality;

public class CensusSubdivision : AuditedAggregateRoot<Guid>
{
    public string CensusSubdivisionName { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string RegionalDistrictCode { get; set; } = string.Empty;

}
