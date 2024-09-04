using System;
using System.Collections.ObjectModel;
using System.Linq;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.Flex.Domain.Scoresheets
{
    public class ScoresheetSection : FullAuditedEntity<Guid>, IMultiTenant
    {
        public virtual string Name { get; set; } = string.Empty;
        public virtual uint Order { get; set; }        
        public Guid? TenantId { get; set; }

        // Navigation
        public virtual Scoresheet? Scoresheet { get; }
        public virtual Guid ScoresheetId { get; set; }

        public virtual Collection<Question> Fields { get; private set; } = [];


        protected ScoresheetSection()
        {
            /* This constructor is for ORMs to be used while getting the entity from the database. */            
        }
        public ScoresheetSection(Guid id,
            string name,
            uint order)
           : base(id)
        {
            Id = id;
            Name = name;
            Order = order;
        }

        public ScoresheetSection(Guid id,
            string name,
            uint order,
            Guid scoresheetId)
           : base(id)
        {
            Id = id;
            Name = name;
            Order = order;  
            ScoresheetId = scoresheetId;
        }

        public ScoresheetSection SetName(string name)
        {
            if (Scoresheet!.Sections.Any(s => s.Name == name && s.Id != Id))
                throw new UserFriendlyException("Cannot duplicate section names.");

            Name = name;
            return this;
        }

        public ScoresheetSection AddQuestion(Question question)
        {
            if (Scoresheet!.Sections.SelectMany(s => s.Fields).Any(s => s.Name == question.Name && s.Id != question.Id))
                throw new UserFriendlyException("Cannot duplicate question names for a scoresheet.");
            question.SectionId = this.Id;
            Fields.Add(question);

            return this;
        }
    }
}
