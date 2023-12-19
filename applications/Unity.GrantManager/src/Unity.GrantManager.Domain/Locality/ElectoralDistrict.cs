using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Unity.GrantManager.Locality;

public class ElectoralDistrict : AuditedAggregateRoot<Guid>
{
    public string ElectoralDistrictName { get; set; } = string.Empty;

    public string ElectoralDistrictCode { get; set; } = string.Empty;

}
