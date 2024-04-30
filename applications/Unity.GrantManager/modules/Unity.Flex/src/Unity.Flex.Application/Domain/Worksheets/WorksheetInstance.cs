using System;
using System.Text.Json;
using Unity.Modules.Shared.Correlation;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.Flex.Domain.Worksheets
{
    public class WorksheetInstance : FullAuditedAggregateRoot<Guid>, IMultiTenant, ICorrelationEntity
    {
        // Current serialized value
        public virtual string Value { get; private set; } = "{}";

        // Navigation
        public Worksheet? Worksheet { get; }
        public Guid WorksheetId { get; set; }

        // Correlation
        public virtual Guid CorrelationId { get; private set; }
        public virtual string CorrelationProvider { get; private set; } = string.Empty;

        public Guid? TenantId { get; set; }

        public WorksheetInstance UpdateValue()
        {
            // this needs to dig and get the sheet + sections + field values
            Value = JsonSerializer.Serialize(this);
            return this;
        }
    }
}
