using System;
using System.ComponentModel.DataAnnotations;

namespace Unity.GrantManager.Assessments
{
    public class UpdateAssessmentCommentDto
    {
        [StringLength(2000)]
        [Required]
        [MinLength(1)]
        public string Comment { get; set; } = string.Empty;

        [Required]
        public Guid CommentId { get; set; }
    }
}
