using System;
using static Volo.Abp.Identity.Settings.IdentitySettingNames;

namespace Unity.GrantManager.Assessments;
public class AssessmentListItemDto
{
    public Guid Id { get; set; }
    public Guid ApplicationId { get; set; }
    public Guid AssessorId { get; set; }
    public string AssessorDisplayName { get; set; } = string.Empty;
    public string AssessorFullName { get; set; } = string.Empty;
    public string AssessorBadge { get; set; } = string.Empty;  
    

    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    public AssessmentState Status { get; set; }
    public bool IsComplete { get; set; }
    public bool? ApprovalRecommended { get; set; }
    
    public double SubTotal {  get; set; }

    public int? SectionScore1 { get; set; }
    public int? SectionScore2 { get; set; }
    public int? SectionScore3 { get; set; }
    public int? SectionScore4 { get; set; }

}
