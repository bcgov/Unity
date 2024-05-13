using System;
using System.Collections.ObjectModel;
using System.Text.Json;
using Unity.Modules.Shared.Correlation;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.Flex.Domain.WorksheetInstances
{
    public class WorksheetInstance : FullAuditedAggregateRoot<Guid>, IMultiTenant, ICorrelationEntity
    {
        public virtual string Value { get; private set; } = "{}";

        public Guid WorksheetId { get; set; }

        // Correlation
        public virtual Guid CorrelationId { get; private set; }
        public virtual string CorrelationProvider { get; private set; } = string.Empty;

        public virtual string CorrelationAnchor { get; private set; } = string.Empty;

        public Guid? TenantId { get; set; }

        public virtual Collection<CustomFieldValue> Values { get; private set; } = [];

        protected WorksheetInstance()
        {
            /* This constructor is for ORMs to be used while getting the entity from the database. */
        }

        public WorksheetInstance(Guid id,
            Guid worksheetId,
            Guid correlationId,
            string correlationProvider,
            string correlationAnchor)
        {
            Id = id;
            CorrelationId = correlationId;
            CorrelationProvider = correlationProvider;
            CorrelationAnchor = correlationAnchor;
            WorksheetId = worksheetId;
        }

        public WorksheetInstance UpdateValue()
        {
            // this needs to dig and get the sheet + sections + field values
            Value = JsonSerializer.Serialize(this);
            return this;
        }
    }
}
