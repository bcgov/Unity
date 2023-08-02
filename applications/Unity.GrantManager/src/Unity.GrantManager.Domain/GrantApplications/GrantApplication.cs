using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Unity.GrantManager.GrantApplications
{
    public class GrantApplication : AuditedAggregateRoot<Guid>
    {
        public string Name { get; set; }
    }
}
