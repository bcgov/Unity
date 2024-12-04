using System;

namespace Unity.Flex.Scoresheets.Events
{
    [Serializable]
    public class AssessmentAnswersEto
    {
        public Guid QuestionId { get; set; }
        public string? Answer { get; set; }
        public int? QuestionType { get; set; }
    }
}
