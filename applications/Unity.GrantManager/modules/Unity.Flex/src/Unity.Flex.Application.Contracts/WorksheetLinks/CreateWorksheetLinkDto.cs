using System;

namespace Unity.Flex.WorksheetLinks
{
    [Serializable]
    public class CreateWorksheetLinkDto
    {
        public Guid WorksheetId { get; set; }
        public Guid CorrelationId { get; set; }
        public string CorrelationProvider { get; set; } = string.Empty;
        public string UiAnchor { get; set; } = string.Empty;
    }
}
