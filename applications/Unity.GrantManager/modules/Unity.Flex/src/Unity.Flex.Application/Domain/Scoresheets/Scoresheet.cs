using System;
using System.Collections.ObjectModel;
using System.Linq;
using Unity.Flex.Domain.Exceptions;
using Unity.Flex.Domain.ScoresheetInstances;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.Flex.Domain.Scoresheets
{
    public class Scoresheet : FullAuditedAggregateRoot<Guid>, IMultiTenant
    {
        public virtual string Title { get; set; } = string.Empty;
        public virtual string Name { get; private set; } = string.Empty;
        public virtual uint Version { get; set; } = 1;
        public virtual uint Order { get; set; } = 0;
        public virtual bool Published {  get; set; } = false;
        public Guid? TenantId { get; set; }
               

        public virtual Collection<ScoresheetSection> Sections { get; private set; } = [];
        public virtual Collection<ScoresheetInstance> Instances { get; private set; } = [];

        protected Scoresheet()
        {
            /* This constructor is for ORMs to be used while getting the entity from the database. */
        }

        public Scoresheet(Guid id,
        string title,
        string name)
        : base(id)
        {
            Id = id;
            Title = title;
            Name = name;
        }

        public Scoresheet AddSection(ScoresheetSection section)
        {
            if (Sections.Any(s => s.Name.Equals(section.Name, StringComparison.CurrentCultureIgnoreCase)))
            {
                throw new UserFriendlyException("Section names must be unique");
            }
            section.ScoresheetId = this.Id;
            Sections.Add(section);
            return this;
        }

        public Scoresheet UpdateSectionName(string name, string newName)
        {
            var section = Sections.FirstOrDefault(s => s.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase));

            if (section != null)
            {
                var sectionByNewName = Sections.FirstOrDefault(s => s.Name.Equals(newName, StringComparison.CurrentCultureIgnoreCase));

                if (sectionByNewName != null)
                {
                    throw new BusinessException(ErrorConsts.DuplicateSectionName).WithData("duplicateName", newName);
                }

                section.SetName(newName);
            }

            return this;
        }

        public Scoresheet DeleteSection(string name)
        {
            var section = Sections.FirstOrDefault(s => s.Name == name);
            if (section != null)
            {
                Sections.Remove(section);
            }
            return this;
        }

        public Scoresheet IncreaseVersion()
        {
            Version++;
            return this;
        }

        public Scoresheet SetName(string name)
        {
            this.Name = name;
            return this;
        }

        public Scoresheet UpdateSection(ScoresheetSection section, string name)
        {
            section.SetName(name);
            return this;
        }
    }
}
