using System;
using System.Collections.ObjectModel;
using System.Text.Json;
using Unity.Flex.Domain.Scoresheets;
using Unity.Modules.Shared.Correlation;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.Flex.Domain.ScoresheetInstances
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

        public virtual Collection<Answer> Answers { get; private set; } = [];

        public ScoresheetInstance(Guid id, 
            Guid scoresheetId, 
            Guid correlationId,
            string correlationProvider)
        {
            Id = id;
            ScoresheetId = scoresheetId;
            CorrelationId = correlationId;
            CorrelationProvider = correlationProvider;
        }
        public ScoresheetInstance UpdateValue()
        {
            // this needs to dig and get the sheet + sections + field values
            Value = JsonSerializer.Serialize(this);
            return this;
        }
    }
}
