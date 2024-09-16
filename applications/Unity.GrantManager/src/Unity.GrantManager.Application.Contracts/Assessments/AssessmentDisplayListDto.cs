using System.Collections.Generic;

namespace Unity.GrantManager.Assessments;
public class AssessmentDisplayListDto
{
    public List<AssessmentListItemDto> Data { get; set; } = [];    
    public bool IsApplicationUsingDefaultScoresheet { get; set; }
}
