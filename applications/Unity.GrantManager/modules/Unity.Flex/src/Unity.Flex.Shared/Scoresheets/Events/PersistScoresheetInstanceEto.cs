using System;

namespace Unity.Flex.Scoresheets.Events
{
    [Serializable]
    public class PersistScoresheetInstanceEto
    {
        public Guid CorrelationId { get; set; }
        public Guid QuestionId { get; set; }
        public string? Answer { get; set; }
        public int QuestionType { get; set; }
    }
}
