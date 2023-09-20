using System;
using Volo.Abp.Application.Dtos;

namespace Unity.GrantManager.Assessments
{
    [Serializable]
    public class AssessmentDto : EntityDto<Guid>
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsComplete { get; set; }
        public string Status { get; set; } = "TBD";
        public bool? ApprovalRecommended { get; set; }

        public string? AdjudicatorName { get; set; }

        public Guid? CreatorId { get; set; }
    }
}

