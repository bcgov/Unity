using System;
using System.ComponentModel.DataAnnotations.Schema;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.Flex.Domain.Scoresheets
{
    public class Answer : FullAuditedEntity<Guid>, IMultiTenant
    {
        [Column(TypeName = "jsonb")]
        public virtual string? CurrentValue { get; set; } = "{}";
        [Column(TypeName = "jsonb")]
        public virtual string? DefaultValue { get; set; } = "{}";
        public uint Version { get; set; }

        public virtual double CurrentScore { get; set; }
        public virtual double DefaultScore { get; set; } = double.NaN;

        // Navigation
        public Question? Question { get; }
        public Guid QuestionId { get; set; }

        public Guid ScoresheetInstanceId { get; set; }

        public Guid? TenantId { get; set; }


        protected Answer()
        {
            /* This constructor is for ORMs to be used while getting the entity from the database. */
        }

        public Answer(Guid id)
        {
            Id = id;
        }
    }
}
