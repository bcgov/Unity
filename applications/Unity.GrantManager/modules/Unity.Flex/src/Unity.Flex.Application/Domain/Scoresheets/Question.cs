using System;
using System.Collections.ObjectModel;
using Unity.Flex.Scoresheets;
using System.ComponentModel.DataAnnotations.Schema;
using Unity.Flex.Worksheets.Definitions;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;
using Newtonsoft.Json;
using Volo.Abp;
using System.Linq;

namespace Unity.Flex.Domain.Scoresheets
{
    public class Question : FullAuditedEntity<Guid>, IMultiTenant
    {
        public virtual string Name { get; private set; } = string.Empty;
        public virtual string Label { get; set; } = string.Empty;
        public virtual string? Description { get; set; }
        public virtual uint Order { get; set; }
        public virtual QuestionType Type { get; set; }
        public virtual bool Enabled { get; private set; }

        public virtual Collection<Answer> Answers { get; private set; } = [];

        // Navigation
        public virtual ScoresheetSection? Section { get; }
        public virtual Guid SectionId { get; set; }

        public Guid? TenantId { get; set; }

        [Column(TypeName = "jsonb")]
        public virtual string Definition { get; set; } = "{}";

        protected Question()
        {
            /* This constructor is for ORMs to be used while getting the entity from the database. */
        }

        [JsonConstructor]
        public Question(Guid id, string name, string label, QuestionType type, uint order, string? description, string? definition)
        {
            Id = id;
            Name = name;
            Label = label;
            Type = type;
            Order = order;
            Description = description;
            Enabled = true;
            Definition = definition ?? "{}";
        }

        public Question(Guid id, string name, string label, QuestionType type, uint order, string? description, Guid sectionId, object? definition)
        {
            Id = id;
            Name = name;
            Label = label;
            Type = type;
            Order = order;
            Description = description;
            SectionId = sectionId;
            Enabled = true;
            Definition = DefinitionResolver.Resolve(type, definition);
        }

        public Question AddAnswer(Answer answer)
        {
            Answers.Add(answer);
            return this;
        }
        
        public Question SetName(string name)
        {
            var scoresheet = Section!.Scoresheet;

            if (scoresheet!.Sections.SelectMany(s => s.Fields).Any(s => s.Name == name && s.Id != Id))
                throw new UserFriendlyException("Cannot duplicate question names for a scoresheet.");
            Name = name;
            return this;
        }
    }
}