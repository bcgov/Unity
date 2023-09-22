using System;
using System.ComponentModel.DataAnnotations;

namespace Unity.GrantManager.Assessments
{
    public class UpdateAssessmentRecommendationDto
    {
      
      
        public bool? ApprovalRecommended { get; set; }

        [Required]
        public Guid AssessmentId { get; set; }
    }
}
