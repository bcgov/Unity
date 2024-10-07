using System.ComponentModel;

namespace Unity.ApplicantPortal.Web.ViewModels;

public class ApplicantSubmissionsViewModel
{

    public Guid SubmissionId { get; set; }

    [DisplayName("Reference #")]
    public string ReferenceNo { get; set; } = string.Empty;

    [DisplayName("Submission Date")]
    public DateTime SubmissionDate { get; set; }

    [DisplayName("Status")]
    public string Status { get; set; } = string.Empty;
}
