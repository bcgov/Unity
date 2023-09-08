using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Unity.GrantManager.Assessments
{
    public class Assessment : AuditedAggregateRoot<Guid>
    {
        public Guid ApplicationId { get; set; }      
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsComplete { get; set; }
        public bool? ApprovalRecommended { get; set; }
    }
}
