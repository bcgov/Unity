using System;

namespace Unity.Flex.Scoresheets.Events
{
    [Serializable]
    public class PersistScoresheetInstanceEto
    {
        public Guid CorrelationId { get; set; }
        public Guid QuestionId { get; set; }
        public double Answer { get; set; }
    }
}
