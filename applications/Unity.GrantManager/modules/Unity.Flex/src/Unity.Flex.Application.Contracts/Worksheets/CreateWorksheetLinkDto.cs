using System;

namespace Unity.Flex.Worksheets
{
    public class CreateWorksheetLinkDto
    {
        public Guid WorksheetId { get; set; }
        public Guid CorrelationId { get; set; }
        public string CorrelationProvider { get; set; } = string.Empty;
    }
}
