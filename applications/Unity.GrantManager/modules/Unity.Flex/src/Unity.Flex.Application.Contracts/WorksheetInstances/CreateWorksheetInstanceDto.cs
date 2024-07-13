using System;

namespace Unity.Flex.WorksheetInstances
{
    [Serializable]
    public class CreateWorksheetInstanceDto
    {
        public Guid WorksheetId { get; set; }
        public Guid CorrelationId { get; set; }
        public string CorrelationProvider { get; set; } = string.Empty;
        public Guid SheetCorrelationId { get; set; }
        public string SheetCorrelationProvider { get; set; } = string.Empty;
        public string CorrelationAnchor { get; set; } = string.Empty;        
    }
}
