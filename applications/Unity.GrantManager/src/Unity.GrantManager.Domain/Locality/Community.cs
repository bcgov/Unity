using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Unity.GrantManager.Locality
{
    public class Community : AuditedAggregateRoot<Guid>
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string RegionalDistrictCode { get; set; } = string.Empty;
    }
}
