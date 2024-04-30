using System;
using System.Collections.ObjectModel;
using Unity.Flex.Domain.Enums;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.Flex.Domain.Scoresheets
{
    public class ScoreField : FullAuditedEntity<Guid>, IMultiTenant
    {
        public virtual string Name { get; private set; } = string.Empty;
        public virtual string Label { get; private set; } = string.Empty;
        public virtual ScoreFieldType Type { get; private set; }
        public virtual bool Enabled { get; private set; }

        public virtual Collection<ScoreFieldValue> Values { get; private set; } = [];

        // Navigation
        public virtual ScoresheetSection? Section { get; }
        public virtual Guid SectionId { get; }

        public Guid? TenantId { get; set; }

        protected ScoreField()
        {
            /* This constructor is for ORMs to be used while getting the entity from the database. */
        }

        public ScoreField(Guid id, string name, string label, ScoreFieldType type)
        {
            Id = id;
            Name = name;
            Label = label;
            Type = type;
            Enabled = true;
        }

        public ScoreField AddValue(ScoreFieldValue value)
        {
            Values.Add(value);
            return this;
        }        
    }
}