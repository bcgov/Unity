using System;
using System.Collections.ObjectModel;
using System.Linq;
using Unity.Flex.Domain.Exceptions;
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
            Name = name;
            return this;
        }

        public ScoresheetSection AddField(Question field)
        {
            if (Fields.Any(s => s.Name.Equals(field.Name, StringComparison.CurrentCultureIgnoreCase)))
            {
                throw new BusinessException(ErrorConsts.DuplicateSectionName).WithData("duplicateName", field.Name); // cannot have duplicate field names
            }
            field.SectionId = this.Id;
            Fields.Add(field);

            return this;
        }
    }
}
