﻿using System;
using System.Collections.ObjectModel;
using Unity.Flex.Domain.Enums;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.Flex.Domain.Scoresheets
{
    public class Question : FullAuditedEntity<Guid>, IMultiTenant
    {
        public virtual string Name { get; set; } = string.Empty;
        public virtual string Label { get; set; } = string.Empty;
        public virtual string? Description { get; set; }
        public virtual uint Order { get; set; }
        public virtual QuestionType Type { get; private set; }
        public virtual bool Enabled { get; private set; }

        public virtual Collection<Answer> Answers { get; private set; } = [];

        // Navigation
        public virtual ScoresheetSection? Section { get; }
        public virtual Guid SectionId { get; set; }

        public Guid? TenantId { get; set; }

        protected Question()
        {
            /* This constructor is for ORMs to be used while getting the entity from the database. */
        }

        public Question(Guid id, string name, string label, QuestionType type, uint order, string? description, Guid sectionId)
        {
            Id = id;
            Name = name;
            Label = label;
            Type = type;
            Order = order;
            Description = description;
            SectionId = sectionId;
            Enabled = true;
        }

        public Question AddAnswer(Answer answer)
        {
            Answers.Add(answer);
            return this;
        }        
    }
}