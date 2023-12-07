using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Unity.GrantManager.Applications;

public class ApplicationEconomicRegion : AuditedAggregateRoot<Guid>
{
    public string EconomicRegionName { get; set; } = string.Empty;

    public string EconomicRegionCode { get; set; } = string.Empty;

}
