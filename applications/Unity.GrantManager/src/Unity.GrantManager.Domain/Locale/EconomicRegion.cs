using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Unity.GrantManager.Locale;

public class EconomicRegion : AuditedAggregateRoot<Guid>
{
    public string EconomicRegionName { get; set; } = string.Empty;

    public string EconomicRegionCode { get; set; } = string.Empty;

}
