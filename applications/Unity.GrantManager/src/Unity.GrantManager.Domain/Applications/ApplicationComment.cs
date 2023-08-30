using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Unity.GrantManager.Applications
{
    public class ApplicationComment : AuditedAggregateRoot<Guid>
    {
        public Guid ApplicationId { get; set; }

        public string Comment { get; set; } = string.Empty;
    }
}
