using System;
using System.Collections.ObjectModel;
using Unity.Flex.Domain.Enums;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.Flex.Domain.Scoresheets
{
    public class Question : FullAuditedEntity<Guid>, IMultiTenant
    {
        public virtual string Name { get; private set; } = string.Empty;
        public virtual string Label { get; private set; } = string.Empty;
        public virtual QuestionType Type { get; private set; }
        public virtual bool Enabled { get; private set; }

        public virtual Collection<QuestionValue> Values { get; private set; } = [];

        // Navigation
        public virtual ScoresheetSection? Section { get; }
        public virtual Guid SectionId { get; }

        public Guid? TenantId { get; set; }

        protected Question()
        {
            /* This constructor is for ORMs to be used while getting the entity from the database. */
        }

        public Question(Guid id, string name, string label, QuestionType type)
        {
            Id = id;
            Name = name;
            Label = label;
            Type = type;
            Enabled = true;
        }

        public Question AddValue(QuestionValue value)
        {
            Values.Add(value);
            return this;
        }        
    }
}