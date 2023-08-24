using System.ComponentModel.DataAnnotations;
namespace Unity.GrantManager.GrantApplications
{
    public class CreateUpdateAssessmentCommentDto
    {

        [StringLength(2000)]
        public string Comment { get; set; } = string.Empty;

        public string? ApplicationFormSubmissionId { get; set; }
    }

}
