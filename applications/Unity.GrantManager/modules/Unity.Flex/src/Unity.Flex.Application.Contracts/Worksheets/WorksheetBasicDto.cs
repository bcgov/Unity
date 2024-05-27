using System;

namespace Unity.Flex.Worksheets
{
    [Serializable]
    public class WorksheetBasicDto
    {
        public Guid Id { get; set; }
        public Guid CorrelationId { get; set; }
        public string CorrelationProvider { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string UiAnchor { get; set; } = string.Empty;
    }
}
