using System;

namespace Unity.Flex.Scoresheets.Events
{
    [Serializable]
    public class CreateScoresheetInstanceEto
    {
        public Guid ScoresheetId { get; set; }
        public Guid CorrelationId { get; set; }
        public string CorrelationProvider { get; set; } = string.Empty;
        public Guid? RelatedCorrelationId { get; set; }
    }
}
