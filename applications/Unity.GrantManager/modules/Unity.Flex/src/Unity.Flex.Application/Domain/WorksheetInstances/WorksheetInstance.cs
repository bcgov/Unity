using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations.Schema;
using Unity.Modules.Shared.Correlation;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.Flex.Domain.WorksheetInstances
{
    public class WorksheetInstance : FullAuditedAggregateRoot<Guid>, IMultiTenant, ICorrelationEntity
    {
        [Column(TypeName = "jsonb")]
        public virtual string CurrentValue { get; private set; } = "{}";

        public Guid WorksheetId { get; set; }

        // Correlation
        public virtual Guid CorrelationId { get; private set; }
        public virtual string CorrelationProvider { get; private set; } = string.Empty;

        public virtual Guid WorksheetCorrelationId { get; private set; }
        public virtual string WorksheetCorrelationProvider { get; private set; } = string.Empty;

        public virtual string UiAnchor { get; private set; } = string.Empty;

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
            Guid worksheetCorrelationId,
            string worksheetCorrelationProvider,
            string correlationAnchor)
        {
            Id = id;
            CorrelationId = correlationId;
            CorrelationProvider = correlationProvider;
            WorksheetCorrelationId = worksheetCorrelationId;
            WorksheetCorrelationProvider = worksheetCorrelationProvider;
            UiAnchor = correlationAnchor;
            WorksheetId = worksheetId;
        }

        public WorksheetInstance AddValue(Guid customFieldId, string currentValue)
        {
            Values.Add(new CustomFieldValue(Guid.NewGuid(), Id, customFieldId, currentValue));
            return this;
        }

        public WorksheetInstance SetValue(string currentValue)
        {
            CurrentValue = currentValue;
            return this;
        }

        public WorksheetInstance SetAnchor(string uiAnchor)
        {
            UiAnchor = uiAnchor;
            return this;
        }
    }
}
