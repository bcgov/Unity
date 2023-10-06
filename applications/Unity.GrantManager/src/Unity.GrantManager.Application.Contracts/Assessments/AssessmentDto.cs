using System;
using Volo.Abp.Application.Dtos;

namespace Unity.GrantManager.Assessments
{
    [Serializable]
    public class AssessmentDto : EntityDto<Guid>
    {
        public Guid ApplicationId { get; set; }
        public Guid AssessorId { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public AssessmentState Status { get; private set; }
        public bool IsComplete { get; set; }
        public bool? ApprovalRecommended { get; set; }
    }
}

