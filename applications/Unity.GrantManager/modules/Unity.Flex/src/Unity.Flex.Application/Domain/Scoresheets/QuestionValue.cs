using System;
using System.ComponentModel.DataAnnotations.Schema;
using Unity.Modules.Shared.Correlation;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.Flex.Domain.Scoresheets
{
    public class QuestionValue : FullAuditedEntity<Guid>, IMultiTenant, ICorrelationEntity
    {
        [Column(TypeName = "jsonb")]
        public virtual string? CurrentValue { get; private set; } = "{}";
        [Column(TypeName = "jsonb")]
        public virtual string? DefaultValue { get; private set; } = "{}";
        public uint Version { get; set; }

        public virtual double CurrentScore { get; private set; }
        public virtual double DefaultScore { get; private set; } = double.NaN;

        // Navigation
        public Question? Question { get; }
        public Guid QuestionId { get; }

        // Correlation
        public virtual Guid CorrelationId { get; private set; }
        public virtual string CorrelationProvider { get; private set; } = string.Empty;

        public Guid? TenantId { get; set; }


        protected QuestionValue()
        {
            /* This constructor is for ORMs to be used while getting the entity from the database. */
        }

        public QuestionValue(Guid id,
            Guid correlationId,
            string correlationProvider)
        {
            Id = id;
            CorrelationId = correlationId;
            CorrelationProvider = correlationProvider;
        }
    }
}
