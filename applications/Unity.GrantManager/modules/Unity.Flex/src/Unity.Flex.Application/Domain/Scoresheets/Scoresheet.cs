using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Unity.Flex.Domain.Exceptions;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.Flex.Domain.Scoresheets
{
    public class Scoresheet : FullAuditedAggregateRoot<Guid>, IMultiTenant
    {
        public virtual string Name { get; set; } = string.Empty;
        public virtual uint Version { get; set; } = 1;

        public Guid GroupId { get; set; }

        public Guid? TenantId { get; set; }

        [NotMapped]
        public Collection<uint> GroupVersions { get; set; }

        public virtual Collection<ScoresheetSection> Sections { get; set; } = [];
        public virtual Collection<ScoresheetInstance> Instances { get; set; } = [];

        protected Scoresheet()
        {
            /* This constructor is for ORMs to be used while getting the entity from the database. */
        }

        public Scoresheet(Guid id,
        string name,
        Guid groupId)
        : base(id)
        {
            Id = id;
            Name = name;
            GroupId = groupId;
        }

        public Scoresheet AddSection(string name, uint order)
        {
            if (Sections.Any(s => s.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase)))
            {
                throw new BusinessException(ErrorConsts.DuplicateSectionName).WithData("duplicateName", name); // cannot have duplicate section names
            }

            Sections.Add(new ScoresheetSection(Guid.NewGuid(), name, order));
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
    }
}
