using System;

namespace Unity.GrantManager.Assessments;
public class AssessmentListItemDto
{
    public Guid Id { get; set; }
    public Guid ApplicationId { get; set; }
    public Guid AssessorId { get; set; }
    public string AssessorDisplayName => $"{AssessorLastName}, {AssessorFirstName}";
    public string AssessorFirstName { get; set; } = string.Empty;
    public string AssessorLastName { get; set; } = string.Empty;
    public string AssessorEmail { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    public AssessmentState Status { get; set; }
    public bool IsComplete { get; set; }
    public bool? ApprovalRecommended { get; set; }
}
