using System;
using System.Collections.Generic;

namespace Unity.GrantManager.Assessments;
public class AssessmentScoreSectionDto
{
    public Guid AssessmentId { get; set; }
    public List<Unity.Flex.Scoresheets.Events.AssessmentAnswersEto> AssessmentAnswers { get; set; } = [];
}

