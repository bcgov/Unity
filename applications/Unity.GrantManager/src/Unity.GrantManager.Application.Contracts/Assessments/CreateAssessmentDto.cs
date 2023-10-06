using System;
using System.ComponentModel.DataAnnotations;

namespace Unity.GrantManager.Assessments
{
    [Serializable]
    public class CreateAssessmentDto
    {
        [Required]
        public Guid ApplicationId { get; set; }
    }
}
