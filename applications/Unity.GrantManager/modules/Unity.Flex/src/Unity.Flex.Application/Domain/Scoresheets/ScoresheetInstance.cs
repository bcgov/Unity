using System;
using System.Text.Json;
using Unity.Modules.Shared.Correlation;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.Flex.Domain.Scoresheets
{
    public class ScoresheetInstance : FullAuditedAggregateRoot<Guid>, IMultiTenant, ICorrelationEntity
    {
        // Current serialized value
        public virtual string Value { get; private set; } = "{}";

        // Navigation
        public Scoresheet? Scoresheet { get; }
        public Guid ScoresheetId { get; set; }

        // Correlation
        public virtual Guid CorrelationId { get; private set; }
        public virtual string CorrelationProvider { get; private set; } = string.Empty;

        public Guid? TenantId { get; set; }

        public ScoresheetInstance UpdateValue()
        {
            // this needs to dig and get the sheet + sections + field values
            Value = JsonSerializer.Serialize(this);
            return this;
        }
    }
}
