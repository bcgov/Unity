using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Unity.GrantManager.Locale;

public class ElectoralDistrict : AuditedAggregateRoot<Guid>
{
    public string ElectoralDistrictName { get; set; } = string.Empty;

    public string ElectoralDistrictCode { get; set; } = string.Empty;

}
