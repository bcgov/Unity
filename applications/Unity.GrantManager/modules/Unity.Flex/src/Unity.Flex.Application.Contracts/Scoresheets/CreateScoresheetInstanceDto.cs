using System;

namespace Unity.Flex.Scoresheets
{
    [Serializable]
    public class CreateScoresheetInstanceDto
    {
        public Guid ScoresheetId { get; set; }
        public Guid CorrelationId { get; set; }
        public string CorrelationProvider { get; set; } = string.Empty;
        public Guid? RelatedCorrelationId { get; set; }
    }
}
